using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TradeJournal.Api.Authentication;

public class FirebaseAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Firebase";

    public FirebaseAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var firebaseToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, firebaseToken.Uid),
                new Claim("uid", firebaseToken.Uid)
            };

            if (firebaseToken.Claims.TryGetValue("email", out var emailObj) && emailObj is string email)
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }

            if (firebaseToken.Claims.TryGetValue("email_verified", out var verifiedObj))
            {
                var verified = verifiedObj is bool b ? b : verifiedObj?.ToString() == "true";
                claims.Add(new Claim("email_verified", verified.ToString()));
            }

            if (firebaseToken.Claims.TryGetValue("name", out var nameObj) && nameObj is string name)
            {
                claims.Add(new Claim(ClaimTypes.Name, name));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException ex)
        {
            Logger.LogWarning(ex, "Firebase token verification failed");
            return AuthenticateResult.Fail("Invalid authentication token.");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Unexpected error during Firebase token verification");
            return AuthenticateResult.Fail("Authentication failed.");
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";
        return Response.WriteAsync("{\"message\":\"Invalid or missing authentication token.\"}");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Response.ContentType = "application/json";
        return Response.WriteAsync("{\"message\":\"You do not have permission to access this resource.\"}");
    }
}
