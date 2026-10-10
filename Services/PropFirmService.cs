using Google.Cloud.Firestore;
using TradeJournal.Api.Models;

namespace TradeJournal.Api.Services;

/// <summary>
/// Dedicated service for the "propFirms" collection.
/// Kept separate from <see cref="FirestoreService"/> so regular trade storage is untouched.
/// </summary>
public class PropFirmService
{
    private const string CollectionName = "propFirms";
    private readonly FirestoreDb _firestoreDb;

    public PropFirmService(FirestoreDb firestoreDb)
    {
        _firestoreDb = firestoreDb;
    }

    public async Task<List<PropFirm>> GetPropFirmsAsync(string userId)
    {
        Query query = _firestoreDb
            .Collection(CollectionName)
            .WhereEqualTo("UserId", userId);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents
            .Select(d => Normalize(d.ConvertTo<PropFirm>()))
            .OrderBy(f => f.FirmName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<PropFirm?> GetPropFirmAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        DocumentSnapshot snapshot = await _firestoreDb
            .Collection(CollectionName)
            .Document(id)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
            return null;

        return Normalize(snapshot.ConvertTo<PropFirm>());
    }

    /// <summary>
    /// Records written before AmountSpent/Status/Payouts existed must still read back cleanly,
    /// so missing or out-of-range values are healed to safe defaults on the way out.
    /// </summary>
    private static PropFirm Normalize(PropFirm propFirm)
    {
        if (propFirm == null!)
            return null!;

        if (double.IsNaN(propFirm.AmountSpent) ||
            double.IsInfinity(propFirm.AmountSpent) ||
            propFirm.AmountSpent < 0)
        {
            propFirm.AmountSpent = 0;
        }

        if (double.IsNaN(propFirm.Payouts) ||
            double.IsInfinity(propFirm.Payouts) ||
            propFirm.Payouts < 0)
        {
            propFirm.Payouts = 0;
        }

        propFirm.Status = NormalizeStatus(propFirm.Status);

        return propFirm;
    }

    /// <summary>
    /// Healing pass for records written before Status existed, or with an unexpected value.
    /// Active, Failed and Passed are all preserved; anything else falls back to Active.
    /// </summary>
    private static string NormalizeStatus(string? status)
    {
        if (string.Equals(status?.Trim(), "Failed", StringComparison.OrdinalIgnoreCase))
            return "Failed";

        if (string.Equals(status?.Trim(), "Passed", StringComparison.OrdinalIgnoreCase))
            return "Passed";

        return "Active";
    }

    public async Task AddPropFirmAsync(PropFirm propFirm)
    {
        await _firestoreDb
            .Collection(CollectionName)
            .Document(propFirm.Id)
            .SetAsync(propFirm);
    }

    public async Task UpdatePropFirmAsync(string id, PropFirm propFirm)
    {
        propFirm.Id = id;

        await _firestoreDb
            .Collection(CollectionName)
            .Document(id)
            .SetAsync(propFirm);
    }

    public async Task DeletePropFirmAsync(string id)
    {
        await _firestoreDb
            .Collection(CollectionName)
            .Document(id)
            .DeleteAsync();
    }
}
