using Google.Cloud.Firestore;

namespace TradeJournal.Api.Models;

[FirestoreData]
public class OtpChallenge
{
    [FirestoreProperty]
    public string ChallengeId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string OtpHash { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTimeOffset CreatedAt { get; set; }

    [FirestoreProperty]
    public DateTimeOffset ExpiresAt { get; set; }

    [FirestoreProperty]
    public int AttemptCount { get; set; }

    [FirestoreProperty]
    public int MaxAttempts { get; set; } = 5;

    [FirestoreProperty]
    public bool Used { get; set; }

    [FirestoreProperty]
    public DateTimeOffset LastSentAt { get; set; }

    [FirestoreProperty]
    public int ResendCount { get; set; }

    [FirestoreProperty]
    public string Purpose { get; set; } = "login";
}
