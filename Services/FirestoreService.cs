using Google.Cloud.Firestore;
using TradeJournal.Api.Models;

namespace TradeJournal.Api.Services;

public class FirestoreService
{
    private readonly FirestoreDb _firestoreDb;

    public FirestoreService(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task AddTradeAsync(Trade trade)
    {
        var collection = _firestoreDb.Collection("trades");
        await collection.Document(trade.Id).SetAsync(trade);
    }

    //public async Task<List<Trade>> GetTradesAsync()
    //{
    //    var snapshot = await _firestoreDb
    //        .Collection("trades")
    //        .GetSnapshotAsync();

    //    var trades = new List<Trade>();

    //    foreach (var document in snapshot.Documents)
    //    {
    //        trades.Add(document.ConvertTo<Trade>());
    //    }

    //    return trades;
    //}

    public async Task<List<Trade>> GetTradesAsync(string userId)
    {
        Query query = _firestoreDb
            .Collection("trades")
            .WhereEqualTo("UserId", userId);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents
            .Select(d => d.ConvertTo<Trade>())
            .ToList();
    }

    public async Task DeleteTradeAsync(string id)
    {
        await _firestoreDb
            .Collection("trades")
            .Document(id)
            .DeleteAsync();
    }

    public async Task UpdateTradeAsync(string id, Trade trade)
    {
        trade.Id = id;

        await _firestoreDb
            .Collection("trades")
            .Document(id)
            .SetAsync(trade);
    }

    public async Task SaveUserProfileAsync(UserProfile profile)
    {
        DocumentReference docRef =
            _firestoreDb.Collection("userProfiles")
                        .Document(profile.Id);

        await docRef.SetAsync(profile);
    }

    public async Task<UserProfile?> GetUserProfileAsync(string userId)
    {
        DocumentSnapshot snapshot =
            await _firestoreDb.Collection("userProfiles")
                              .Document(userId)
                              .GetSnapshotAsync();

        if (!snapshot.Exists)
            return null;

        return snapshot.ConvertTo<UserProfile>();
    }

    public async Task<Trade?> GetTradeByIdAsync(string id)
    {
        DocumentReference docRef =
            _firestoreDb.Collection("trades").Document(id);

        DocumentSnapshot snapshot =
            await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
            return null;

        return snapshot.ConvertTo<Trade>();
    }
}