using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
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

    /// <summary>The resolved switch for this batch: the kill-switch mode, and the request's override where the mode allows it.</summary>
    public bool UsePrecomputedGenotypeSets { get; }

    public UsePrecomputedGenotypeSetsSource UsePrecomputedGenotypeSetsSource { get; }

    /// <summary>The kill-switch mode the batch ran under.</summary>
    public PrecomputedGenotypeSetMode Mode { get; }

    /// <summary>The name of the allowed-loci key that was looked up; null when none was.</summary>
    public string AllowedLociKey { get; }

    public int? MatchingAlgorithmDataRefreshRecordId { get; }

    /// <summary>
    /// Null when the rows were read. Otherwise the reason every donor in the batch is computed live, including
    /// <see cref="DonorGenotypeSetSource.PrecomputeDisabled"/> when the switch is off.
    /// </summary>
    public DonorGenotypeSetSource? BatchFallbackReason { get; }

    private readonly IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> rows;

    /// <summary>The batch's patient key (see <see cref="IdentifiedMatchProbabilityRequest.PatientGenotypeSetKey"/>); null when it has none.</summary>
    public PatientGenotypeSetKey PatientGenotypeSetKey { get; }

    private readonly PrecomputedPatientGenotypeSetRow patientRow;

    /// <summary>
    /// How long the read of the batch's stored rows took, with the choice of the donor inputs' frequency sets before it
    /// (ATL-424); null when there was no read (ATL-474).
    /// </summary>
    public TimeSpan? ReadTime { get; }

    /// <summary>
    /// The number of stored rows read for the batch's donor inputs, not counting the patient's row. Each row is counted
    /// once: the ids of a donor input, and inputs with one key, share one row (ATL-424).
    /// </summary>
    public int ReadRowCount { get; }

    /// <summary>The payload bytes read for the batch: each donor row once, and the patient's row when it is not a donor row.</summary>
    public long ReadBytes { get; }

    /// <summary>
    /// True when a donor computed live should be stored for reuse: the switch is on, the rows were read, and so the
    /// transient database matching used was still active at the start of the batch. The writer checks that again
    /// just before it writes.
    /// </summary>
    public bool CanStore => BatchFallbackReason == null && MatchingAlgorithmDataRefreshRecordId != null;

    /// <summary>
    /// False only when the batch has a stored row for the patient and for every donor input - so it should impute
    /// nothing live, and has no need to warm the caches live imputation reads (ATL-476).
    /// </summary>
    /// <remarks>
    /// A cheap check, not <see cref="TryGetPrecomputedGenotypeSet"/>: it does not compare frequency sets or decode. So it
    /// can say false for a batch that then computes a donor live after all (a frequency set activated during the batch,
    /// or a row that cannot be decoded - both rare). That donor's lookups then fall back to single-row reads, as they did
    /// before the warm-up existed. Whenever a donor input or the patient has no row at all, it says true.
    /// </remarks>
    public bool MayComputeLive(IReadOnlyCollection<DonorInput> donors) =>
        BatchFallbackReason != null
        || patientRow == null
        || donors.Any(donor => !donor.DonorIds.Any(rows.ContainsKey));

    private DonorGenotypeSetBatchContext(
        bool usePrecomputedGenotypeSets,
        UsePrecomputedGenotypeSetsSource source,
        PrecomputedGenotypeSetMode mode,
        string allowedLociKey,
        int? matchingAlgorithmDataRefreshRecordId,
        DonorGenotypeSetSource? batchFallbackReason,
        IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> rows,
        PatientGenotypeSetKey patientGenotypeSetKey,
        PrecomputedPatientGenotypeSetRow patientRow,
        TimeSpan? readTime)
    {
        UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets;
        UsePrecomputedGenotypeSetsSource = source;
        Mode = mode;
        AllowedLociKey = allowedLociKey;
        MatchingAlgorithmDataRefreshRecordId = matchingAlgorithmDataRefreshRecordId;
        BatchFallbackReason = batchFallbackReason;
        this.rows = rows ?? NoRows;
        PatientGenotypeSetKey = patientGenotypeSetKey;
        this.patientRow = patientRow;
        ReadTime = readTime;

        // The ids that share a row share one object, and the patient's payload is a donor row's when their keys are equal.
        var distinctRows = this.rows.Values.Distinct<PrecomputedDonorGenotypeSetRow>(ReferenceEqualityComparer.Instance).ToList();
        ReadRowCount = distinctRows.Count;
        ReadBytes = distinctRows
            .Select(row => row.SubjectGenotypeSetData)
            .Append(patientRow?.SubjectGenotypeSetData)
            .Where(data => data != null)
            .Distinct<byte[]>(ReferenceEqualityComparer.Instance)
            .Sum(data => (long)data.Length);
    }

    public static DonorGenotypeSetBatchContext Disabled(
        UsePrecomputedGenotypeSetsSource source,
        PrecomputedGenotypeSetMode mode,
        int? matchingAlgorithmDataRefreshRecordId,
        PatientGenotypeSetKey patientGenotypeSetKey = null) =>
        new(false, source, mode, null, matchingAlgorithmDataRefreshRecordId, DonorGenotypeSetSource.PrecomputeDisabled, NoRows,
            patientGenotypeSetKey, null, null);

    /// <param name="readTime">How long <paramref name="lookup"/> took to read; logged only.</param>
    public static DonorGenotypeSetBatchContext Enabled(
        UsePrecomputedGenotypeSetsSource source,
        PrecomputedGenotypeSetMode mode,
        int? matchingAlgorithmDataRefreshRecordId,
        PrecomputedDonorGenotypeSetLookup lookup,
        PatientGenotypeSetKey patientGenotypeSetKey = null,
        TimeSpan? readTime = null) =>
        new(true, source, mode, lookup.AllowedLociKey, matchingAlgorithmDataRefreshRecordId, lookup.BatchFallbackReason, lookup.Rows,
            patientGenotypeSetKey, lookup.PatientRow, readTime);

    /// <summary>
    /// Finds and decodes the stored genotype set of a donor input.
    /// </summary>
    /// <param name="donorIds">The ids that share the donor input's phenotype and frequency set metadata.</param>
    /// <param name="donorFrequencySetId">The donor frequency set this search uses for them.</param>
    /// <param name="genotypeSet">The decoded set when the result is <see cref="DonorGenotypeSetSource.Precomputed"/>; otherwise null.</param>
    /// <remarks>
    /// <para>
    /// The ids of a donor input share one row: the stored value with the key of the input's typing (the typing that
    /// matching read), frequency set and allowed loci (ATL-424). So the row is right for the typing the search has,
    /// whatever an import did to the donor after matching.
    /// </para>
    ///
    /// <para>
    /// The row is used only while its frequency set is the one the search uses for the donor now. The batch chose the
    /// set when it started, and a new set can be activated before the donor runs. A row that fails this check is never
    /// used - not its payload and not its <c>IsUnrepresented</c> flag.
    /// </para>
    /// </remarks>
    public DonorGenotypeSetSource TryGetPrecomputedGenotypeSet(
        IReadOnlyCollection<int> donorIds,
        int donorFrequencySetId,
        out SubjectGenotypeSet genotypeSet) =>
        TryGetPrecomputedGenotypeSet(donorIds, donorFrequencySetId, out genotypeSet, out _);

    /// <inheritdoc cref="TryGetPrecomputedGenotypeSet(IReadOnlyCollection{int}, int, out SubjectGenotypeSet)"/>
    /// <param name="storedBytes">
    /// The size of the payload of the row that was used, for logging (ATL-474); 0 when no row was used, or the row is unrepresented.
    /// </param>
    public DonorGenotypeSetSource TryGetPrecomputedGenotypeSet(
        IReadOnlyCollection<int> donorIds,
        int donorFrequencySetId,
        out SubjectGenotypeSet genotypeSet,
        out int storedBytes)
    {
        genotypeSet = null;
        storedBytes = 0;

        if (BatchFallbackReason != null)
        {
            return BatchFallbackReason.Value;
        }

        var row = donorIds.Select(rows.GetValueOrDefault).FirstOrDefault(found => found != null);
        if (row == null)
        {
            return DonorGenotypeSetSource.NoRow;
        }

        if (row.HaplotypeFrequencySetId != donorFrequencySetId)
        {
            return DonorGenotypeSetSource.StaleFrequencySet;
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
            storedBytes = row.SubjectGenotypeSetData.Length;
            return DonorGenotypeSetSource.Precomputed;
        }
        catch (InvalidDataException)
        {
            // Rare by construction - the payload is versioned and the decoder is the encoder's tested inverse -
            // but one bad row must cost one live imputation, not the search.
            return DonorGenotypeSetSource.DecodeFailed;
        }
    }

    /// <summary>
    /// Finds and decodes the batch's stored patient genotype set.
    /// </summary>
    /// <param name="patientFrequencySetId">The patient frequency set this batch uses.</param>
    /// <param name="genotypeSet">The decoded set when the result is <see cref="PatientGenotypeSetSource.Precomputed"/>; otherwise null.</param>
    /// <remarks>
    /// The row is used only when it was stored with the frequency set the batch uses: a new set can be activated after
    /// the warm step. The typing is not checked again: the key was built from this search's patient typing.
    /// </remarks>
    public PatientGenotypeSetSource TryGetPrecomputedPatientGenotypeSet(int patientFrequencySetId, out SubjectGenotypeSet genotypeSet)
    {
        genotypeSet = null;

        if (BatchFallbackReason != null)
        {
            return BatchFallbackReason.Value switch
            {
                DonorGenotypeSetSource.UncoveredAllowedLoci => PatientGenotypeSetSource.UncoveredAllowedLoci,
                DonorGenotypeSetSource.ActiveDatabaseChanged => PatientGenotypeSetSource.ActiveDatabaseChanged,
                DonorGenotypeSetSource.ActiveDatabaseUnknown => PatientGenotypeSetSource.ActiveDatabaseUnknown,
                DonorGenotypeSetSource.ReadFailed => PatientGenotypeSetSource.ReadFailed,
                _ => PatientGenotypeSetSource.PrecomputeDisabled
            };
        }

        if (PatientGenotypeSetKey == null)
        {
            return PatientGenotypeSetSource.NoKey;
        }

        if (PatientGenotypeSetKey.HaplotypeFrequencySetId != patientFrequencySetId)
        {
            return PatientGenotypeSetSource.StaleFrequencySet;
        }

        if (patientRow == null)
        {
            return PatientGenotypeSetSource.NoRow;
        }

        if (patientRow.IsUnrepresented)
        {
            genotypeSet = new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0m);
            return PatientGenotypeSetSource.Precomputed;
        }

        if (patientRow.SubjectGenotypeSetData == null)
        {
            return PatientGenotypeSetSource.DecodeFailed;
        }

        try
        {
            genotypeSet = SubjectGenotypeSetPayload.Decode(patientRow.SubjectGenotypeSetData);
            return PatientGenotypeSetSource.Precomputed;
        }
        catch (InvalidDataException)
        {
            return PatientGenotypeSetSource.DecodeFailed;
        }
    }
}
