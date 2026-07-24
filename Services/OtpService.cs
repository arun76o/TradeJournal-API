using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TradeJournal.Api.Models;

namespace TradeJournal.Api.Services;

public class OtpService : IOtpService
{
    private readonly TwoFactorOptions _options;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOptions<TwoFactorOptions> options,
        ILogger<OtpService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GenerateOtp()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var number = BitConverter.ToUInt32(bytes) % 1_000_000;
        return number.ToString("D6");
    }

    public string HashOtp(string otp, string pepper)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(otp));
        return Convert.ToBase64String(hash);
    }

    public bool VerifyOtp(string submittedOtp, string storedHash, string pepper)
    {
        var computedHash = HashOtp(submittedOtp, pepper);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    public string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "****";

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        if (name.Length <= 2)
            return $"*{name[1..]}@{domain}";

        return $"{name[0]}***@{domain}";
    }

    public async Task<OtpChallenge> CreateChallengeAsync(string userId, string purpose, string pepper)
    {
        var challenge = new OtpChallenge
        {
            ChallengeId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.OtpExpiryMinutes),
            AttemptCount = 0,
            MaxAttempts = _options.MaxAttempts,
            Used = false,
            LastSentAt = DateTimeOffset.UtcNow,
            ResendCount = 0,
            Purpose = purpose
        };

        var otp = GenerateOtp();
        challenge.OtpHash = HashOtp(otp, pepper);

        return challenge;
    }

    public Task<OtpChallenge?> GetChallengeAsync(string challengeId)
    {
        return Task.FromResult<OtpChallenge?>(null);
    }

    public Task SaveChallengeAsync(OtpChallenge challenge)
    {
        return Task.CompletedTask;
    }

    public Task InvalidatePreviousChallengesAsync(string userId, string purpose)
    {
        return Task.CompletedTask;
    }

    public Task<bool> IsResendCooldownMetAsync(OtpChallenge challenge, int cooldownSeconds)
    {
        var elapsed = DateTimeOffset.UtcNow - challenge.LastSentAt;
        return Task.FromResult(elapsed.TotalSeconds >= cooldownSeconds);
    }

    public string GenerateSessionToken(TwoFactorSession session)
    {
        var payload = $"{session.UserId}|{session.Email}|{session.IssuedAt.ToUnixTimeSeconds()}|{session.ExpiresAt.ToUnixTimeSeconds()}|{session.Purpose}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sigBase64 = Convert.ToBase64String(signature);

        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        return $"{payloadBase64}.{sigBase64}";
    }

    public TwoFactorSession? ValidateSessionToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        try
        {
            var payloadBytes = Convert.FromBase64String(parts[0]);
            var payload = Encoding.UTF8.GetString(payloadBytes);
            var payloadParts = payload.Split('|');
            if (payloadParts.Length != 5)
                return null;

            var userId = payloadParts[0];
            var email = payloadParts[1];
            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(payloadParts[2]));
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(payloadParts[3]));
            var purpose = payloadParts[4];

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningKey));
            var expectedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var expectedSigBase64 = Convert.ToBase64String(expectedSig);

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSigBase64),
                Encoding.UTF8.GetBytes(parts[1])))
            {
                return null;
            }

            if (DateTimeOffset.UtcNow > expiresAt)
                return null;

            return new TwoFactorSession
            {
                UserId = userId,
                Email = email,
                IssuedAt = issuedAt,
                ExpiresAt = expiresAt,
                Purpose = purpose
            };
        }
        catch
        {
            return null;
        }
    }
}

public class TwoFactorSession
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Purpose { get; set; } = "2fa-verified";
}
