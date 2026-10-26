using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace Atlas.MatchPrediction.Services.HaplotypeFrequencies;

/// <summary>
/// Writes the content hash of each haplotype frequency set that has none: the sets of the imports from before content
/// hashes. Until then, such a set has a cache entry of its own, which it shares with no other set.
/// </summary>
public interface IFrequencySetContentHashBackfill
{
    /// <summary>
    /// Writes the content hash of each set that has none, one set at a time: the active sets first, then the inactive
    /// ones, which a rollback can activate. It reads the rows of each set, as a load does. It stops between two sets when
    /// <paramref name="cancellationToken"/> is cancelled, so a later run continues it.
    /// </summary>
    /// <returns>The number of sets that this run hashed.</returns>
    Task<int> Run(CancellationToken cancellationToken);

    Task<HaplotypeFrequencySetContentHashCounts> GetCounts();
}

internal class FrequencySetContentHashBackfill(
    IHaplotypeFrequencySetRepository setRepository,
    IHaplotypeFrequenciesRepository frequenciesRepository,
    ILogger<FrequencySetContentHashBackfill> logger) : IFrequencySetContentHashBackfill
{
    /// <inheritdoc />
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        var setIds = await setRepository.GetIdsOfSetsWithNoContentHash();
        logger.LogInformation("Content hash backfill of the haplotype frequency sets: {SetCount} sets have no content hash.", setIds.Count);

        var setsHashed = 0;
        foreach (var setId in setIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Content hash backfill of the haplotype frequency sets: stopped after {SetsHashed} sets.", setsHashed);
                break;
            }

            var stopwatch = Stopwatch.StartNew();
            var rows = await frequenciesRepository.GetAllHaplotypeFrequencies(setId);
            var readMilliseconds = stopwatch.ElapsedMilliseconds;

            var contentHash = FrequencySetContentHash.Calculate(rows);

            // Only if the set still has no hash: an import of the set, which can run at the same time, writes the hash of all
            // its rows, and a backfill that read the set during that import must not replace it.
            var written = await setRepository.SetContentHashIfMissing(setId, contentHash);
            setsHashed++;

            logger.LogInformation(
                "Content hash of HF set {SetId}: {Rows} rows read in {ReadMilliseconds} ms, content hash {ContentHash}. {Outcome}",
                setId,
                rows.Count,
                readMilliseconds,
                FrequencySetContentHash.ToHex(contentHash),
                written ? "Written." : "Not written: the set had a hash already.");
        }

        var counts = await setRepository.GetContentHashCounts();
        logger.LogInformation(
            "Content hash backfill of the haplotype frequency sets: {SetsHashed} sets hashed in this run. {SetsWithNoContentHash} of {Sets} sets have no content hash. The active sets have {ContentHashesOfActiveSets} distinct content hashes, and all the sets have {ContentHashes}.",
            setsHashed,
            counts.SetsWithNoContentHash,
            counts.Sets,
            counts.ContentHashesOfActiveSets,
            counts.ContentHashes);

        return setsHashed;
    }

    /// <inheritdoc />
    public Task<HaplotypeFrequencySetContentHashCounts> GetCounts() => setRepository.GetContentHashCounts();
}
