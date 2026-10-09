using Google.Cloud.Firestore;

namespace TradeJournal.Api.Models;

[FirestoreData]
public class PropFirmTrade
{
    [FirestoreProperty]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string PropFirmId { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Symbol { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Side { get; set; } = string.Empty;

    [FirestoreProperty]
    public double EntryPrice { get; set; }

    [FirestoreProperty]
    public double ExitPrice { get; set; }

    [FirestoreProperty]
    public double StopLoss { get; set; }

    [FirestoreProperty]
    public double TakeProfit { get; set; }

    [FirestoreProperty]
    public double Quantity { get; set; }

    [FirestoreProperty]
    public string Strategy { get; set; } = string.Empty;

    [FirestoreProperty]
    public DateTime TradeDate { get; set; }

    [FirestoreProperty]
    public string Notes { get; set; } = string.Empty;

    [FirestoreProperty]
    public double GrossProfitUSD { get; set; }

    [FirestoreProperty]
    public double GrossProfitINR { get; set; }

    // Net profit expressed in the prop firm account currency.
    [FirestoreProperty]
    public double NetProfit { get; set; }

    // Gross profit expressed in the prop firm account currency.
    [FirestoreProperty]
    public double GrossProfit { get; set; }

    // Fees expressed in the prop firm account currency.
    [FirestoreProperty]
    public double Fees { get; set; }

    [FirestoreProperty]
    public double NetProfitINR { get; set; }

    [FirestoreProperty]
    public List<string> ImageUrls { get; set; } = new();

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty]
    public DateTime UpdatedAt { get; set; }
}
