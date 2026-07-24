using TradeJournal.Api.Models;

namespace TradeJournal.Api.Services;

public interface IOtpService
{
    string GenerateOtp();
    string HashOtp(string otp, string pepper);
    bool VerifyOtp(string submittedOtp, string storedHash, string pepper);
    string MaskEmail(string email);
    Task<OtpChallenge> CreateChallengeAsync(string userId, string purpose, string pepper);
    Task<OtpChallenge?> GetChallengeAsync(string challengeId);
    Task SaveChallengeAsync(OtpChallenge challenge);
    Task InvalidatePreviousChallengesAsync(string userId, string purpose);
    Task<bool> IsResendCooldownMetAsync(OtpChallenge challenge, int cooldownSeconds);
    string GenerateSessionToken(TwoFactorSession session);
    TwoFactorSession? ValidateSessionToken(string token);
}
