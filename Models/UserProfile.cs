namespace TradeJournal.Api.Models
{
    using Google.Cloud.Firestore;

    [FirestoreData]
    public class UserProfile
    {
        [FirestoreProperty]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty]
        public string DisplayName { get; set; } = string.Empty;

        [FirestoreProperty]
        public string Email { get; set; } = string.Empty;

        [FirestoreProperty]
        public string TradingGoal { get; set; } = string.Empty;

        [FirestoreProperty]
        public string PreferredMarkets { get; set; } = string.Empty;

        [FirestoreProperty]
        public double MaxDailyLoss { get; set; }

        [FirestoreProperty]
        public double MaxRiskPerTrade { get; set; }

        [FirestoreProperty]
        public string? ProfileImageUrl { get; set; }

        [FirestoreProperty]
        public bool TwoFactorEnabled { get; set; }
    }
}
