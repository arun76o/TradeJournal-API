using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TradeJournal.Api.Models;
using TradeJournal.Api.Services;

namespace TradeJournal.Api.Authorization;

public class TwoFactorRequirement : IAuthorizationRequirement { }

public class TwoFactorAuthorizationHandler : AuthorizationHandler<TwoFactorRequirement>
{
    private readonly FirestoreService _firestoreService;
    private readonly IOtpService _otpService;
    private readonly TwoFactorOptions _options;
    private readonly ILogger<TwoFactorAuthorizationHandler> _logger;

    public TwoFactorAuthorizationHandler(
        FirestoreService firestoreService,
        IOtpService otpService,
        IOptions<TwoFactorOptions> options,
        ILogger<TwoFactorAuthorizationHandler> logger)
    {
        _firestoreService = firestoreService;
        _otpService = otpService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TwoFactorRequirement requirement)
    {
        var uid = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(uid))
        {
            context.Fail(new AuthorizationFailureReason(this, "Not authenticated."));
            return;
        }

        var twoFactorEnabled = await _firestoreService.GetTwoFactorEnabledAsync(uid);
        if (!twoFactorEnabled)
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
        if (httpContext == null)
        {
            context.Fail(new AuthorizationFailureReason(this, "Invalid request context."));
            return;
        }

        var twoFactorToken = httpContext.Request.Headers["X-2FA-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(twoFactorToken))
        {
            context.Fail(new AuthorizationFailureReason(this, "Two-factor authentication required."));
            return;
        }

        var session = _otpService.ValidateSessionToken(twoFactorToken);
        if (session == null || session.UserId != uid)
        {
            context.Fail(new AuthorizationFailureReason(this, "Invalid two-factor session."));
            return;
        }

        context.Succeed(requirement);
    }
}
