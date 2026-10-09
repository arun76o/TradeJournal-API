using Google.Cloud.Firestore;

namespace TradeJournal.Api.Models;

[FirestoreData]
public class PropFirm
{
    [FirestoreProperty]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string FirmName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string AccountName { get; set; } = string.Empty;

    [FirestoreProperty]
    public string AccountId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string AccountType { get; set; } = string.Empty;

    [FirestoreProperty]
    public double InitialBalance { get; set; }

    [FirestoreProperty]
    public string AccountCurrency { get; set; } = "USD";

    [FirestoreProperty]
    public double? AccountSize { get; set; }

    /// <summary>
    /// Profit target expressed as a PERCENTAGE of the account size (e.g. 8 = 8%).
    /// The currency amount is derived for display only and is never stored here.
    /// </summary>
    [FirestoreProperty]
    public double? ProfitTarget { get; set; }

    [FirestoreProperty]
    public double? MaximumDailyLoss { get; set; }

    /// <summary>
    /// Overall drawdown limit expressed as a PERCENTAGE of the account size (e.g. 10 = 10%).
    /// The currency amount is derived for display only and is never stored here.
    /// </summary>
    [FirestoreProperty]
    public double? MaximumOverallDrawdown { get; set; }

    /// <summary>
    /// User-entered expense for this firm, always reported in the journal's INR reporting
    /// currency. It is never derived from trades, balance or P/L.
    /// </summary>
    [FirestoreProperty]
    public double AmountSpent { get; set; }

    /// <summary>
    /// Manual lifecycle flag: "Active" or "Failed". Never changed automatically from P/L.
    /// </summary>
    [FirestoreProperty]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Total payouts actually received from the firm, denominated in
    /// <see cref="AccountCurrency"/>. Manual entry only: never derived from trades,
    /// never changed when a trade is added or removed.
    /// </summary>
    [FirestoreProperty]
    public double Payouts { get; set; }

    [FirestoreProperty]
    public DateTime? StartDate { get; set; }

    [FirestoreProperty]
    public string Notes { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty]
    public DateTime UpdatedAt { get; set; }
}
