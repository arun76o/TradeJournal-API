using Google.Cloud.Firestore;
using TradeJournal.Api.Models;

namespace TradeJournal.Api.Services;

/// <summary>
/// Dedicated service for the "propFirmTrades" collection.
/// Prop firm trades are never written to the regular "trades" collection.
/// </summary>
public class PropFirmTradeService
{
    private const string CollectionName = "propFirmTrades";
    private readonly FirestoreDb _firestoreDb;

    public PropFirmTradeService(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<List<PropFirmTrade>> GetTradesAsync(string propFirmId, string userId)
    {
        if (string.IsNullOrWhiteSpace(propFirmId))
            return new List<PropFirmTrade>();

        Query query = _firestoreDb
            .Collection(CollectionName)
            .WhereEqualTo("PropFirmId", propFirmId);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents
            .Select(d => d.ConvertTo<PropFirmTrade>())
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TradeDate)
            .ToList();
    }

    public async Task<PropFirmTrade?> GetTradeAsync(string propFirmId, string tradeId)
    {
        if (string.IsNullOrWhiteSpace(tradeId))
            return null;

        DocumentSnapshot snapshot = await _firestoreDb
            .Collection(CollectionName)
            .Document(tradeId)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
            return null;

        var trade = snapshot.ConvertTo<PropFirmTrade>();

        if (!string.IsNullOrWhiteSpace(propFirmId) && trade.PropFirmId != propFirmId)
            return null;

        return trade;
    }

    public async Task<int> CountTradesAsync(string propFirmId, string userId)
    {
        var trades = await GetTradesAsync(propFirmId, userId);
        return trades.Count;
    }

    public async Task AddTradeAsync(PropFirmTrade trade)
    {
        await _firestoreDb
            .Collection(CollectionName)
            .Document(trade.Id)
            .SetAsync(trade);
    }

    public async Task UpdateTradeAsync(string id, PropFirmTrade trade)
    {
        trade.Id = id;

        await _firestoreDb
            .Collection(CollectionName)
            .Document(id)
            .SetAsync(trade);
    }

    public async Task DeleteTradeAsync(string id)
    {
        await _firestoreDb
            .Collection(CollectionName)
            .Document(id)
            .DeleteAsync();
    }

    /// <summary>
    /// Deletes every prop firm trade belonging to a firm. Only called after the user has
    /// explicitly confirmed the deletion policy for a firm that still has trades.
    /// </summary>
    public async Task<int> DeleteTradesForFirmAsync(string propFirmId, string userId)
    {
        var trades = await GetTradesAsync(propFirmId, userId);
        if (trades.Count == 0)
            return 0;

        // Firestore batches are limited to 500 operations.
        for (var start = 0; start < trades.Count; start += 400)
        {
            var batch = _firestoreDb.StartBatch();
            foreach (var trade in trades.Skip(start).Take(400))
            {
                batch.Delete(
                    _firestoreDb.Collection(CollectionName).Document(trade.Id));
            }
            await batch.CommitAsync();
        }

        return trades.Count;
    }
}
