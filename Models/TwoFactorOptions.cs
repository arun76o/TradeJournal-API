namespace TradeJournal.Api.Models;

public class TwoFactorOptions
{
    public const string SectionName = "TwoFactor";

    public string SigningKey { get; set; } = string.Empty;
    public int SessionExpiryHours { get; set; } = 12;
    public int OtpExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxResendRequests { get; set; } = 5;
    public int ResendWindowMinutes { get; set; } = 15;
}
