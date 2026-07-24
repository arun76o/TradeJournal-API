namespace TradeJournal.Api.Models;

public class StartTwoFactorRequest
{
    public string Purpose { get; set; } = "login";
}

public class StartTwoFactorResponse
{
    public bool TwoFactorRequired { get; set; }
    public string? ChallengeId { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAvailableInSeconds { get; set; }
    public string? MaskedEmail { get; set; }
}

public class VerifyTwoFactorRequest
{
    public string ChallengeId { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
}

public class VerifyTwoFactorResponse
{
    public bool Success { get; set; }
    public string? SessionToken { get; set; }
    public string? Message { get; set; }
}

public class ResendTwoFactorRequest
{
    public string ChallengeId { get; set; } = string.Empty;
}

public class ResendTwoFactorResponse
{
    public int ExpiresInSeconds { get; set; }
    public int ResendAvailableInSeconds { get; set; }
    public string? Message { get; set; }
}

public class TwoFactorStatusResponse
{
    public bool Enabled { get; set; }
}

public class EnableTwoFactorResponse
{
    public string? ChallengeId { get; set; }
    public int ExpiresInSeconds { get; set; }
    public int ResendAvailableInSeconds { get; set; }
    public string? MaskedEmail { get; set; }
}

public class DisableTwoFactorRequest
{
}

public class TwoFactorSessionToken
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Purpose { get; set; } = "2fa-verified";
}
