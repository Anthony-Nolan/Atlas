using System.Collections.Generic;
using System.IO;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>
/// What one match prediction donor batch knows about precomputed genotype sets, resolved once before its donors run
/// (see <see cref="IDonorGenotypeSetSourceResolver"/>). Immutable, so the parallel path can share it across the
/// per-donor scopes.
/// </summary>
public sealed class DonorGenotypeSetBatchContext
{
    private static readonly IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> NoRows = new Dictionary<int, PrecomputedDonorGenotypeSetRow>();

    /// <summary>The resolved switch for this batch: the request's override, or else the kill-switch.</summary>
    public bool UsePrecomputedGenotypeSets { get; }

    public UsePrecomputedGenotypeSetsSource UsePrecomputedGenotypeSetsSource { get; }

    /// <summary>The name of the allowed-loci key that was looked up; null when none was.</summary>
    public string AllowedLociKey { get; }

    public int? MatchingAlgorithmDataRefreshRecordId { get; }

    /// <summary>
    /// Null when the rows were read. Otherwise the reason every donor in the batch is computed live, including
    /// <see cref="DonorGenotypeSetSource.PrecomputeDisabled"/> when the switch is off.
    /// </summary>
    public DonorGenotypeSetSource? BatchFallbackReason { get; }

    private readonly IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> rows;

    /// <summary>
    /// True when a donor computed live should be stored for reuse: the switch is on, the rows were read, and so the
    /// transient database matching used was still active at the start of the batch. The writer checks that again
    /// just before it writes.
    /// </summary>
    public bool CanStore => BatchFallbackReason == null && MatchingAlgorithmDataRefreshRecordId != null;

    private DonorGenotypeSetBatchContext(
        bool usePrecomputedGenotypeSets,
        UsePrecomputedGenotypeSetsSource source,
        string allowedLociKey,
        int? matchingAlgorithmDataRefreshRecordId,
        DonorGenotypeSetSource? batchFallbackReason,
        IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> rows)
    {
        UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets;
        UsePrecomputedGenotypeSetsSource = source;
        AllowedLociKey = allowedLociKey;
        MatchingAlgorithmDataRefreshRecordId = matchingAlgorithmDataRefreshRecordId;
        BatchFallbackReason = batchFallbackReason;
        this.rows = rows ?? NoRows;
    }

    public static DonorGenotypeSetBatchContext Disabled(UsePrecomputedGenotypeSetsSource source, int? matchingAlgorithmDataRefreshRecordId) =>
        new(false, source, null, matchingAlgorithmDataRefreshRecordId, DonorGenotypeSetSource.PrecomputeDisabled, NoRows);

    public static DonorGenotypeSetBatchContext Enabled(
        UsePrecomputedGenotypeSetsSource source,
        int? matchingAlgorithmDataRefreshRecordId,
        PrecomputedDonorGenotypeSetLookup lookup) =>
        new(true, source, lookup.AllowedLociKey, matchingAlgorithmDataRefreshRecordId, lookup.BatchFallbackReason, lookup.Rows);

    /// <summary>
    /// Finds and decodes the stored genotype set of a donor input.
    /// </summary>
    /// <param name="donorIds">The ids that share the donor input's phenotype and frequency set metadata.</param>
    /// <param name="donorFrequencySetId">The donor frequency set this search uses for them.</param>
    /// <param name="genotypeSet">The decoded set when the result is <see cref="DonorGenotypeSetSource.Precomputed"/>; otherwise null.</param>
    /// <remarks>
    /// Any one of the ids is enough: they share a typing, and a row whose frequency set is the one search uses was
    /// computed from that typing and that set. A row for a different set is never used - not its payload and not its
    /// <c>IsUnrepresented</c> flag - because the set it was computed with is no longer the one search would use.
    /// </remarks>
    public DonorGenotypeSetSource TryGetPrecomputedGenotypeSet(
        IReadOnlyCollection<int> donorIds,
        int donorFrequencySetId,
        out SubjectGenotypeSet genotypeSet)
    {
        genotypeSet = null;

        if (BatchFallbackReason != null)
        {
            return BatchFallbackReason.Value;
        }

        var foundAnyRow = false;
        foreach (var donorId in donorIds)
        {
            if (!rows.TryGetValue(donorId, out var row))
            {
                continue;
            }

            foundAnyRow = true;
            if (row.HaplotypeFrequencySetId != donorFrequencySetId)
            {
                continue;
            }

            if (row.IsUnrepresented)
            {
                genotypeSet = new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m);
                return DonorGenotypeSetSource.Precomputed;
            }

            if (row.SubjectGenotypeSetData == null)
            {
                return DonorGenotypeSetSource.DecodeFailed;
            }

            try
            {
                genotypeSet = SubjectGenotypeSetPayload.Decode(row.SubjectGenotypeSetData);
                return DonorGenotypeSetSource.Precomputed;
            }
            catch (InvalidDataException)
            {
                // Rare by construction - the payload is versioned and the decoder is the encoder's tested inverse -
                // but one bad row must cost one live imputation, not the search.
                return DonorGenotypeSetSource.DecodeFailed;
            }
        }

        return foundAnyRow ? DonorGenotypeSetSource.StaleFrequencySet : DonorGenotypeSetSource.NoRow;
    }
}
