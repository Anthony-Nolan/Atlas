using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.Services.Precompute;

/// <summary>
/// Where a donor's genotype set came from in a search: <see cref="Precomputed"/>, or the reason it was computed live.
/// </summary>
/// <remarks>
/// The names are logged as metric names (see <c>DonorGenotypeSetBatchCompleter</c>), so renaming one is a change to
/// what testers and support query in App Insights.
/// </remarks>
public enum DonorGenotypeSetSource
{
    /// <summary>The stored set was decoded and used. No imputation, truncation or conversion ran for this donor.</summary>
    Precomputed,

    /// <summary>None of the donor's ids has a stored row for the search's allowed loci.</summary>
    NoRow,

    /// <summary>
    /// A stored row exists for the donor's typing, but it was computed with a different haplotype frequency set from
    /// the one search now uses.
    /// </summary>
    StaleFrequencySet,

    /// <summary>
    /// The only stored rows were computed from a different typing from the one the search has: the donor was updated
    /// after matching read it. Never stored back, because the donor's current typing is not the one computed here.
    /// </summary>
    TypingChanged,

    /// <summary>
    /// A stored row could not be decoded. Not stored back: the bad row has the same key as the live set, so the next
    /// Data Refresh, which rebuilds the tables, is what repairs it.
    /// </summary>
    DecodeFailed,

    /// <summary>The search's allowed loci are not one of the four precomputed combinations. Applies to the whole batch.</summary>
    UncoveredAllowedLoci,

    /// <summary>
    /// The transient database that matching used is no longer the active one. The dormant database is never read or
    /// written, so the whole batch is computed live.
    /// </summary>
    ActiveDatabaseChanged,

    /// <summary>
    /// The request does not say which transient database matching used: a search matched before ATL-433, or a request
    /// that did not come from a search. Applies to the whole batch.
    /// </summary>
    ActiveDatabaseUnknown,

    /// <summary>
    /// Reading the stored rows failed. Reading is only ever a saving, so the whole batch is computed live rather than
    /// failed, and the failure is logged.
    /// </summary>
    ReadFailed,

    /// <summary>The precomputed path is off for this request: by the kill-switch, or by the request's override.</summary>
    PrecomputeDisabled
}

/// <summary>
/// Where a batch's patient genotype set came from: <see cref="Precomputed"/>, or the reason it was computed live.
/// </summary>
/// <remarks>Logged by name (see <c>DonorGenotypeSetBatchCompleter</c>), like <see cref="DonorGenotypeSetSource"/>.</remarks>
public enum PatientGenotypeSetSource
{
    /// <summary>The stored set was decoded and used. The patient was not imputed for this batch.</summary>
    Precomputed,

    /// <summary>
    /// The batch has no <see cref="IdentifiedMatchProbabilityRequest.PatientGenotypeSetKey"/>: the search started with the
    /// precomputed path off, or the warm step could not store the set.
    /// </summary>
    NoKey,

    /// <summary>The key is set, but no stored row has it.</summary>
    NoRow,

    /// <summary>
    /// The stored set was computed with a different haplotype frequency set from the one the batch now uses for the
    /// patient (a new set was activated after the warm step).
    /// </summary>
    StaleFrequencySet,

    /// <summary>The stored row could not be decoded.</summary>
    DecodeFailed,

    /// <summary>The search's allowed loci are not one of the four precomputed combinations.</summary>
    UncoveredAllowedLoci,

    /// <summary>The transient database that matching used is no longer the active one.</summary>
    ActiveDatabaseChanged,

    /// <summary>The request does not say which transient database matching used.</summary>
    ActiveDatabaseUnknown,

    /// <summary>Reading the stored rows failed.</summary>
    ReadFailed,

    /// <summary>The precomputed path is off for this batch: by the kill-switch, or by the request's override.</summary>
    PrecomputeDisabled
}

/// <summary>What decided whether the precomputed path was used for a batch.</summary>
public enum UsePrecomputedGenotypeSetsSource
{
    /// <summary><c>UsePrecomputedGenotypeSets</c> was set on the request, and the mode allowed it to decide.</summary>
    Request,

    /// <summary>
    /// The kill-switch mode decided: the request left the override null, or the mode is
    /// <see cref="ExternalInterface.Settings.PrecomputedGenotypeSetMode.ForceLive"/>, which ignores it.
    /// </summary>
    FeatureFlag
}

/// <summary>
/// One donor's stored genotype set, as read from the transient database. <see cref="SubjectGenotypeSetData"/> is null
/// exactly when <see cref="IsUnrepresented"/> is true.
/// </summary>
/// <param name="ComputedFromSearchTyping">
/// True when the row's typing key is the key of the typing the search has for this donor. A row for any other typing
/// must not be used: the stored set follows the donor's current typing, which an import can change after matching.
/// </param>
public sealed record PrecomputedDonorGenotypeSetRow(
    int HaplotypeFrequencySetId,
    bool IsUnrepresented,
    byte[] SubjectGenotypeSetData,
    bool ComputedFromSearchTyping);

/// <summary>
/// The stored genotype sets of a batch of donors, or the batch-wide reason there are none to use.
/// </summary>
/// <param name="AllowedLociKey">The name of the allowed-loci key that was looked up; null when the loci are not covered.</param>
/// <param name="BatchFallbackReason">
/// Null when the rows were read. Otherwise <see cref="DonorGenotypeSetSource.UncoveredAllowedLoci"/>,
/// <see cref="DonorGenotypeSetSource.ActiveDatabaseChanged"/> or <see cref="DonorGenotypeSetSource.ActiveDatabaseUnknown"/>,
/// and <paramref name="Rows"/> is empty.
/// </param>
/// <param name="Rows">Stored rows by Atlas donor id. Donors with no row are absent.</param>
/// <param name="PatientRow">
/// The stored row of the patient key that was asked for; null when no key was asked for, when it has no row, or when
/// the key is for a different allowed-loci key from <paramref name="AllowedLociKey"/>.
/// </param>
public sealed record PrecomputedDonorGenotypeSetLookup(
    string AllowedLociKey,
    DonorGenotypeSetSource? BatchFallbackReason,
    IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> Rows,
    PrecomputedPatientGenotypeSetRow PatientRow = null)
{
    public static PrecomputedDonorGenotypeSetLookup Unavailable(DonorGenotypeSetSource reason, string allowedLociKey = null) =>
        new(allowedLociKey, reason, new Dictionary<int, PrecomputedDonorGenotypeSetRow>());
}

/// <summary>
/// A patient's stored genotype set. <see cref="SubjectGenotypeSetData"/> is null exactly when
/// <see cref="IsUnrepresented"/> is true. The haplotype frequency set and typing are part of the key it was read by.
/// </summary>
public sealed record PrecomputedPatientGenotypeSetRow(bool IsUnrepresented, byte[] SubjectGenotypeSetData);

/// <summary>The result of looking up a patient's stored genotype set before it is computed.</summary>
/// <param name="Key">The key of the patient's set; null when <paramref name="UnavailableReason"/> is set.</param>
/// <param name="Exists">True when a row with <paramref name="Key"/> is already stored.</param>
/// <param name="UnavailableReason">
/// Null when the lookup ran. Otherwise <see cref="DonorGenotypeSetSource.UncoveredAllowedLoci"/>,
/// <see cref="DonorGenotypeSetSource.ActiveDatabaseChanged"/> or <see cref="DonorGenotypeSetSource.ActiveDatabaseUnknown"/>.
/// </param>
public sealed record PatientGenotypeSetLookup(PatientGenotypeSetKey Key, bool Exists, DonorGenotypeSetSource? UnavailableReason)
{
    public static PatientGenotypeSetLookup Unavailable(DonorGenotypeSetSource reason) => new(null, false, reason);
}

/// <summary>
/// Reads stored donor genotype sets for search. Implemented by the matching algorithm, which owns the transient
/// databases and the typing key (<c>Atlas.MatchingAlgorithm</c>, registered by <c>RegisterPrecomputedDonorGenotypeSetAccess</c>).
/// </summary>
public interface IPrecomputedDonorGenotypeSetReader
{
    /// <param name="donors">
    /// The batch's donor inputs. Each row is checked against the typing of the input its donor id belongs to, which is
    /// the typing that matching read.
    /// </param>
    /// <param name="allowedLoci">The loci match prediction runs on for this request.</param>
    /// <param name="matchingAlgorithmDataRefreshRecordId">
    /// The data refresh record whose transient database matching used. Rows are read only while that record is still
    /// the active one.
    /// </param>
    /// <param name="patientGenotypeSetKey">
    /// When set, the patient's stored row is read too, under the same check, into
    /// <see cref="PrecomputedDonorGenotypeSetLookup.PatientRow"/>.
    /// </param>
    Task<PrecomputedDonorGenotypeSetLookup> GetDonorGenotypeSets(
        IReadOnlyCollection<DonorInput> donors,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId,
        PatientGenotypeSetKey patientGenotypeSetKey);

    /// <summary>
    /// Builds the key of a patient's genotype set and checks whether a row with it is already stored. Used once per
    /// search, before the batches are uploaded.
    /// </summary>
    /// <param name="patientHla">The patient's typing.</param>
    /// <param name="haplotypeFrequencySetId">The frequency set the patient's set is (or will be) computed with.</param>
    /// <param name="allowedLoci">The loci match prediction runs on for this search.</param>
    /// <param name="matchingAlgorithmDataRefreshRecordId">As for <see cref="GetDonorGenotypeSets"/>.</param>
    Task<PatientGenotypeSetLookup> FindPatientGenotypeSet(
        PhenotypeInfo<string> patientHla,
        int haplotypeFrequencySetId,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId);
}

/// <summary>
/// A donor genotype set that search computed live and that should be stored, so the next search can reuse it.
/// </summary>
/// <param name="DonorIds">The Atlas donor ids that share this phenotype and frequency set. Each gets an assignment row.</param>
/// <param name="DonorHla">The typing the set was computed from. The writer stores it only for donors whose typing is still this.</param>
/// <param name="RegistryCode">The registry code the frequency set was chosen with. The writer stores it only for donors whose registry code is still this.</param>
/// <param name="EthnicityCode">The ethnicity code the frequency set was chosen with. The writer stores it only for donors whose ethnicity code is still this.</param>
/// <param name="HaplotypeFrequencySetId">The donor frequency set the set was computed with.</param>
/// <param name="IsUnrepresented">True when imputation found no genotypes.</param>
/// <param name="SubjectGenotypeSetData">The encoded set; null exactly when <paramref name="IsUnrepresented"/> is true.</param>
public sealed record DonorGenotypeSetToStore(
    IReadOnlyCollection<int> DonorIds,
    PhenotypeInfo<string> DonorHla,
    string RegistryCode,
    string EthnicityCode,
    int HaplotypeFrequencySetId,
    bool IsUnrepresented,
    byte[] SubjectGenotypeSetData);

/// <summary>Counts of donor ids, after a store.</summary>
/// <param name="StoredCount">Donor ids whose assignment now points at the stored set.</param>
/// <param name="SkippedDonorChangedCount">
/// Donor ids skipped because their typing, registry code or ethnicity code changed after matching read them.
/// </param>
/// <param name="SkippedDatabaseChangedCount">Donor ids skipped because the transient database matching used is no longer active.</param>
public sealed record DonorGenotypeSetStoreResult(int StoredCount, int SkippedDonorChangedCount, int SkippedDatabaseChangedCount)
{
    public static DonorGenotypeSetStoreResult None { get; } = new(0, 0, 0);
}

/// <summary>
/// Stores donor genotype sets that search computed live. Implemented by the matching algorithm.
/// </summary>
/// <remarks>
/// May throw: the caller treats storing as best effort, so a failure is logged and never fails the search.
/// </remarks>
public interface IPrecomputedDonorGenotypeSetWriter
{
    /// <param name="genotypeSets">The sets to store.</param>
    /// <param name="allowedLoci">The loci the sets were computed for.</param>
    /// <param name="matchingAlgorithmDataRefreshRecordId">
    /// The data refresh record whose transient database matching used. Nothing is written unless it is still the active one.
    /// </param>
    Task<DonorGenotypeSetStoreResult> Store(
        IReadOnlyCollection<DonorGenotypeSetToStore> genotypeSets,
        IReadOnlySet<Locus> allowedLoci,
        int matchingAlgorithmDataRefreshRecordId);

    /// <summary>
    /// Stores a patient's genotype set as a value row only: no donor points at it. Safe to repeat: a row that already
    /// has <paramref name="key"/> is kept as it is.
    /// </summary>
    /// <param name="key">The key from <see cref="IPrecomputedDonorGenotypeSetReader.FindPatientGenotypeSet"/>.</param>
    /// <param name="isUnrepresented">True when imputation found no genotypes.</param>
    /// <param name="subjectGenotypeSetData">The encoded set; null exactly when <paramref name="isUnrepresented"/> is true.</param>
    /// <param name="matchingAlgorithmDataRefreshRecordId">
    /// The data refresh record whose transient database matching used. Nothing is written unless it is still the active one.
    /// </param>
    /// <returns>True when the row is stored; false when the database matching used is no longer active.</returns>
    Task<bool> StorePatientGenotypeSet(
        PatientGenotypeSetKey key,
        bool isUnrepresented,
        byte[] subjectGenotypeSetData,
        int matchingAlgorithmDataRefreshRecordId);
}

/// <summary>
/// The default for hosts with no access to the transient matching databases (e.g. the standalone match prediction
/// Functions app). Every donor then has <see cref="DonorGenotypeSetSource.NoRow"/> and is computed live.
/// </summary>
internal class NoOpPrecomputedDonorGenotypeSetReader : IPrecomputedDonorGenotypeSetReader
{
    public Task<PrecomputedDonorGenotypeSetLookup> GetDonorGenotypeSets(
        IReadOnlyCollection<DonorInput> donors,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId,
        PatientGenotypeSetKey patientGenotypeSetKey) =>
        Task.FromResult(new PrecomputedDonorGenotypeSetLookup(null, null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()));

    public Task<PatientGenotypeSetLookup> FindPatientGenotypeSet(
        PhenotypeInfo<string> patientHla,
        int haplotypeFrequencySetId,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId) =>
        Task.FromResult(PatientGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseUnknown));
}

/// <summary>The default for hosts with no access to the transient matching databases: stores nothing.</summary>
internal class NoOpPrecomputedDonorGenotypeSetWriter : IPrecomputedDonorGenotypeSetWriter
{
    public Task<DonorGenotypeSetStoreResult> Store(
        IReadOnlyCollection<DonorGenotypeSetToStore> genotypeSets,
        IReadOnlySet<Locus> allowedLoci,
        int matchingAlgorithmDataRefreshRecordId) =>
        Task.FromResult(DonorGenotypeSetStoreResult.None);

    public Task<bool> StorePatientGenotypeSet(
        PatientGenotypeSetKey key,
        bool isUnrepresented,
        byte[] subjectGenotypeSetData,
        int matchingAlgorithmDataRefreshRecordId) =>
        Task.FromResult(false);
}
