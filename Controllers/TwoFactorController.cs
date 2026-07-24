using System.Security.Claims;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

namespace TradeJournal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TwoFactorController : ControllerBase
{
    private readonly FirestoreService _firestoreService;
    private readonly IBrevoEmailService _brevoEmailService;
    private readonly IOtpService _otpService;
    private readonly TwoFactorOptions _twoFactorOptions;
    private readonly ILogger<TwoFactorController> _logger;

    public TwoFactorController(
        FirestoreService firestoreService,
        IBrevoEmailService brevoEmailService,
        IOtpService otpService,
        IOptions<TwoFactorOptions> twoFactorOptions,
        ILogger<TwoFactorController> logger)
    {
        _firestoreService = firestoreService;
        _brevoEmailService = brevoEmailService;
        _otpService = otpService;
        _twoFactorOptions = twoFactorOptions.Value;
        _logger = logger;
    }

    private string GetUid() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string GetEmail() => User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    private string GetPepper() => _twoFactorOptions.SigningKey + "_otp_pepper";
    private string MaskEmail(string email) => _otpService.MaskEmail(email);

    [Authorize]
    [HttpPost("start")]
    public async Task<IActionResult> StartTwoFactor([FromBody] StartTwoFactorRequest request)
    {
        var uid = GetUid();
        var email = GetEmail();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "No email address associated with this account." });

        var twoFactorEnabled = await _firestoreService.GetTwoFactorEnabledAsync(uid);

        if (!twoFactorEnabled && request.Purpose == "login")
        {
            return Ok(new StartTwoFactorResponse
            {
                TwoFactorRequired = false
            });
        }

        await _firestoreService.InvalidatePreviousChallengesAsync(uid, request.Purpose);

        var challenge = await _otpService.CreateChallengeAsync(uid, request.Purpose, GetPepper());
        await _firestoreService.SaveChallengeAsync(challenge);

        var otp = _otpService.GenerateOtp();
        challenge.OtpHash = _otpService.HashOtp(otp, GetPepper());
        await _firestoreService.UpdateChallengeAsync(challenge);

        try
        {
            await _brevoEmailService.SendOtpEmailAsync(email, otp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email");
            return StatusCode(500, new { message = "Failed to send verification email. Please try again." });
        }

        _logger.LogInformation("OTP challenge created for user with purpose {Purpose}", request.Purpose);

        return Ok(new StartTwoFactorResponse
        {
            TwoFactorRequired = true,
            ChallengeId = challenge.ChallengeId,
            ExpiresInSeconds = _twoFactorOptions.OtpExpiryMinutes * 60,
            ResendAvailableInSeconds = _twoFactorOptions.ResendCooldownSeconds,
            MaskedEmail = MaskEmail(email)
        });
    }

    [Authorize]
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var uid = GetUid();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var challenge = await _firestoreService.GetChallengeAsync(request.ChallengeId);
        if (challenge == null)
            return BadRequest(new { message = "Invalid verification challenge." });

        if (challenge.UserId != uid)
            return Forbid();

        if (challenge.Used)
            return BadRequest(new { message = "This verification code has already been used." });

        if (DateTimeOffset.UtcNow > challenge.ExpiresAt)
            return BadRequest(new { message = "This verification code has expired." });

        if (challenge.AttemptCount >= challenge.MaxAttempts)
            return BadRequest(new { message = "Too many incorrect attempts. Request a new verification code." });

        var pepper = GetPepper();
        var isValid = _otpService.VerifyOtp(request.Otp, challenge.OtpHash, pepper);

        if (!isValid)
        {
            challenge.AttemptCount++;
            await _firestoreService.UpdateChallengeAsync(challenge);

            var remaining = challenge.MaxAttempts - challenge.AttemptCount;
            if (remaining <= 0)
                return BadRequest(new { message = "Too many incorrect attempts. Request a new verification code." });

            return BadRequest(new { message = $"Incorrect verification code. {remaining} attempt(s) remaining." });
        }

        challenge.Used = true;
        await _firestoreService.UpdateChallengeAsync(challenge);

        if (challenge.Purpose == "enable-2fa")
        {
            await _firestoreService.SetTwoFactorEnabledAsync(uid, true);
        }
        else if (challenge.Purpose == "disable-2fa")
        {
            await _firestoreService.SetTwoFactorEnabledAsync(uid, false);
        }

        var email = GetEmail();

        var session = new TwoFactorSession
        {
            UserId = uid,
            Email = email,
            IssuedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(_twoFactorOptions.SessionExpiryHours),
            Purpose = "2fa-verified"
        };

        var sessionToken = _otpService.GenerateSessionToken(session);

        _logger.LogInformation("OTP verified successfully with purpose {Purpose}", challenge.Purpose);

        return Ok(new VerifyTwoFactorResponse
        {
            Success = true,
            SessionToken = sessionToken,
            Message = "Verification successful."
        });
    }

    [Authorize]
    [HttpPost("resend")]
    public async Task<IActionResult> ResendTwoFactor([FromBody] ResendTwoFactorRequest request)
    {
        var uid = GetUid();
        var email = GetEmail();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "No email address associated with this account." });

        var challenge = await _firestoreService.GetChallengeAsync(request.ChallengeId);
        if (challenge == null)
            return BadRequest(new { message = "Invalid verification challenge." });

        if (challenge.UserId != uid)
            return Forbid();

        if (challenge.Used)
            return BadRequest(new { message = "This challenge has already been completed." });

        var cooldownMet = await _otpService.IsResendCooldownMetAsync(challenge, _twoFactorOptions.ResendCooldownSeconds);
        if (!cooldownMet)
        {
            var elapsed = (DateTimeOffset.UtcNow - challenge.LastSentAt).TotalSeconds;
            var remaining = (int)(_twoFactorOptions.ResendCooldownSeconds - elapsed);
            return BadRequest(new { message = $"Please wait {remaining} seconds before requesting a new code." });
        }

        if (challenge.ResendCount >= _twoFactorOptions.MaxResendRequests)
            return BadRequest(new { message = "Too many resend requests. Please try again later." });

        var newOtp = _otpService.GenerateOtp();
        challenge.OtpHash = _otpService.HashOtp(newOtp, GetPepper());
        challenge.LastSentAt = DateTimeOffset.UtcNow;
        challenge.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_twoFactorOptions.OtpExpiryMinutes);
        challenge.AttemptCount = 0;
        challenge.ResendCount++;
        await _firestoreService.UpdateChallengeAsync(challenge);

        try
        {
            await _brevoEmailService.SendOtpEmailAsync(email, newOtp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend OTP email");
            return StatusCode(500, new { message = "Failed to send verification email. Please try again." });
        }

        return Ok(new ResendTwoFactorResponse
        {
            ExpiresInSeconds = _twoFactorOptions.OtpExpiryMinutes * 60,
            ResendAvailableInSeconds = _twoFactorOptions.ResendCooldownSeconds,
            Message = "A new verification code has been sent."
        });
    }

    [Authorize]
    [HttpGet("status")]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var uid = GetUid();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var enabled = await _firestoreService.GetTwoFactorEnabledAsync(uid);
        return Ok(new TwoFactorStatusResponse { Enabled = enabled });
    }

    [Authorize]
    [HttpPost("enable/start")]
    public async Task<IActionResult> StartEnableTwoFactor()
    {
        var uid = GetUid();
        var email = GetEmail();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "No email address associated with this account." });

        var alreadyEnabled = await _firestoreService.GetTwoFactorEnabledAsync(uid);
        if (alreadyEnabled)
            return BadRequest(new { message = "Two-factor authentication is already enabled." });

        await _firestoreService.InvalidatePreviousChallengesAsync(uid, "enable-2fa");

        var challenge = await _otpService.CreateChallengeAsync(uid, "enable-2fa", GetPepper());
        await _firestoreService.SaveChallengeAsync(challenge);

        var otp = _otpService.GenerateOtp();
        challenge.OtpHash = _otpService.HashOtp(otp, GetPepper());
        await _firestoreService.UpdateChallengeAsync(challenge);

        try
        {
            await _brevoEmailService.SendOtpEmailAsync(email, otp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send enable-2fa OTP email");
            return StatusCode(500, new { message = "Failed to send verification email. Please try again." });
        }

        return Ok(new EnableTwoFactorResponse
        {
            ChallengeId = challenge.ChallengeId,
            ExpiresInSeconds = _twoFactorOptions.OtpExpiryMinutes * 60,
            ResendAvailableInSeconds = _twoFactorOptions.ResendCooldownSeconds,
            MaskedEmail = MaskEmail(email)
        });
    }

    [Authorize]
    [HttpPost("disable")]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var uid = GetUid();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var currentlyEnabled = await _firestoreService.GetTwoFactorEnabledAsync(uid);

        if (!currentlyEnabled)
            return BadRequest(new { message = "Two-factor authentication is not enabled." });

        await _firestoreService.SetTwoFactorEnabledAsync(uid, false);

        _logger.LogInformation("Two-factor authentication disabled");

        return Ok(new { message = "Two-factor authentication has been disabled." });
    }

    [Authorize]
    [HttpPost("disable-with-verify")]
    public async Task<IActionResult> DisableWithVerification([FromBody] VerifyTwoFactorRequest request)
    {
        var uid = GetUid();

        if (string.IsNullOrWhiteSpace(uid))
            return Unauthorized(new { message = "Invalid or missing authentication token." });

        var challenge = await _firestoreService.GetChallengeAsync(request.ChallengeId);
        if (challenge == null)
            return BadRequest(new { message = "Invalid verification challenge." });

        if (challenge.UserId != uid)
            return Forbid();

        if (challenge.Used)
            return BadRequest(new { message = "This verification code has already been used." });

        if (DateTimeOffset.UtcNow > challenge.ExpiresAt)
            return BadRequest(new { message = "This verification code has expired." });

        if (challenge.AttemptCount >= challenge.MaxAttempts)
            return BadRequest(new { message = "Too many incorrect attempts. Request a new verification code." });

        if (challenge.Purpose != "disable-2fa")
            return BadRequest(new { message = "Invalid verification purpose." });

        var pepper = GetPepper();
        var isValid = _otpService.VerifyOtp(request.Otp, challenge.OtpHash, pepper);

        if (!isValid)
        {
            challenge.AttemptCount++;
            await _firestoreService.UpdateChallengeAsync(challenge);
            var remaining = challenge.MaxAttempts - challenge.AttemptCount;
            if (remaining <= 0)
                return BadRequest(new { message = "Too many incorrect attempts. Request a new verification code." });
            return BadRequest(new { message = $"Incorrect verification code. {remaining} attempt(s) remaining." });
        }

        challenge.Used = true;
        await _firestoreService.UpdateChallengeAsync(challenge);

        await _firestoreService.SetTwoFactorEnabledAsync(uid, false);

        _logger.LogInformation("Two-factor authentication disabled via OTP");

        return Ok(new { message = "Two-factor authentication has been disabled." });
    }
}
