using Atlas.Common.Caching;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Where <see cref="HaplotypeFrequencyCache"/> gets the frequencies of a set: the key of their entry in the cache of the
/// process, and the key of their value in the distributed cache. It comes from the row of the set.
/// </summary>
/// <param name="SetId">The id of the set. A load from the database reads the rows of this set.</param>
/// <param name="LocalKey">
/// The key of the entry in the cache of the process, and in the residency tracker. Sets with the same content hash have
/// the same key, so they share one entry.
/// </param>
/// <param name="DistributedKey">The key of the value in the distributed cache; null when the set does not use it.</param>
/// <param name="ContentHash">
/// The content hash of the entry: the rows of the entry must have it. Null when the entry is for this set only: the set
/// has no content hash, or its rows do not have the hash in its row.
/// </param>
/// <param name="Active">Whether the set is active: only an active set writes to the distributed cache.</param>
/// <param name="RowVersion">
/// The import time and the content hash of the row that gave this source: another value means that a restore or a copy of
/// the database put another set under the id.
/// </param>
/// <param name="SetLocalKey">The key of an entry of this set only: the key of <see cref="ForThisSetOnly"/>.</param>
internal sealed record FrequencySetSource(
    int SetId,
    string LocalKey,
    DistributedCacheKey? DistributedKey,
    byte[] ContentHash,
    bool Active,
    string RowVersion,
    string SetLocalKey)
{
    /// <summary>The set id, and the start of the content hash if there is one: for the logs.</summary>
    public string Description => ContentHash == null ? $"{SetId}" : $"{SetId} (content {FrequencySetContentHash.ToHex(ContentHash)[..12]})";

    /// <summary>
    /// The source of a set whose rows do not have the hash in its row: an entry of its own, which shares nothing, and no
    /// distributed cache.
    /// </summary>
    public FrequencySetSource ForThisSetOnly() => this with { LocalKey = SetLocalKey, DistributedKey = null, ContentHash = null };
}
