using Google.Cloud.Firestore;

namespace TradeJournal.Api.Models;

[FirestoreData]
public class Trade
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;
    [FirestoreProperty]
    public string Id { get; set; } = Guid.NewGuid().ToString();

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
    public DateTime TradeDate { get; set; }

    [FirestoreProperty]
    public string Notes { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Strategy { get; set; } = string.Empty;

    [FirestoreProperty]
    public double GrossProfitUSD { get; set; }

    [FirestoreProperty]
    public double GrossProfitINR { get; set; }

    [FirestoreProperty]
    public double Fees { get; set; }

    [FirestoreProperty]
    public double NetProfitINR { get; set; }

    //[FirestoreProperty]
    //public string ImageUrl { get; set; } = string.Empty;

    [FirestoreProperty]
    public List<string> ImageUrls { get; set; } = new();
}

