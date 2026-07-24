namespace TradeJournal.Api.Services;

public interface IBrevoEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otp);
}
