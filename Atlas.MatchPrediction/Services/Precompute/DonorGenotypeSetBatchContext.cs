using System.Collections.Generic;
using System.IO;
using System.Linq;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
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

    /// <summary>Whether the batch uses precomputed genotype sets: the request's value (see <see cref="MatchProbabilityRequestBase.UsePrecomputedGenotypeSets"/>).</summary>
    public bool UsePrecomputedGenotypeSets { get; }

    /// <summary>The name of the allowed-loci key that was looked up; null when none was.</summary>
    public string AllowedLociKey { get; }

    public int? MatchingAlgorithmDataRefreshRecordId { get; }

    /// <summary>
    /// Null when the rows were read. Otherwise the reason every donor in the batch is computed live, including
    /// <see cref="DonorGenotypeSetSource.PrecomputeDisabled"/> when the request turns the path off.
    /// </summary>
    public DonorGenotypeSetSource? BatchFallbackReason { get; }

    private readonly IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> rows;

    /// <summary>The batch's patient key (see <see cref="IdentifiedMatchProbabilityRequest.PatientGenotypeSetKey"/>); null when it has none.</summary>
    public PatientGenotypeSetKey PatientGenotypeSetKey { get; }

    private readonly PrecomputedPatientGenotypeSetRow patientRow;

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
        string allowedLociKey,
        int? matchingAlgorithmDataRefreshRecordId,
        DonorGenotypeSetSource? batchFallbackReason,
        IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> rows,
        PatientGenotypeSetKey patientGenotypeSetKey,
        PrecomputedPatientGenotypeSetRow patientRow)
    {
        UsePrecomputedGenotypeSets = usePrecomputedGenotypeSets;
        AllowedLociKey = allowedLociKey;
        MatchingAlgorithmDataRefreshRecordId = matchingAlgorithmDataRefreshRecordId;
        BatchFallbackReason = batchFallbackReason;
        this.rows = rows ?? NoRows;
        PatientGenotypeSetKey = patientGenotypeSetKey;
        this.patientRow = patientRow;
    }

    public static DonorGenotypeSetBatchContext Disabled(
        int? matchingAlgorithmDataRefreshRecordId,
        PatientGenotypeSetKey patientGenotypeSetKey = null) =>
        new(false, null, matchingAlgorithmDataRefreshRecordId, DonorGenotypeSetSource.PrecomputeDisabled, NoRows,
            patientGenotypeSetKey, null);

    public static DonorGenotypeSetBatchContext Enabled(
        int? matchingAlgorithmDataRefreshRecordId,
        PrecomputedDonorGenotypeSetLookup lookup,
        PatientGenotypeSetKey patientGenotypeSetKey = null) =>
        new(true, lookup.AllowedLociKey, matchingAlgorithmDataRefreshRecordId, lookup.BatchFallbackReason, lookup.Rows,
            patientGenotypeSetKey, lookup.PatientRow);

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
        out SubjectGenotypeSet genotypeSet)
    {
        genotypeSet = null;

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
