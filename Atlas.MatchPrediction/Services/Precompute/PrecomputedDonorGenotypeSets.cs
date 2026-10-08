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
public sealed record PrecomputedDonorGenotypeSetLookup(
    string AllowedLociKey,
    DonorGenotypeSetSource? BatchFallbackReason,
    IReadOnlyDictionary<int, PrecomputedDonorGenotypeSetRow> Rows)
{
    public static PrecomputedDonorGenotypeSetLookup Unavailable(DonorGenotypeSetSource reason, string allowedLociKey = null) =>
        new(allowedLociKey, reason, new Dictionary<int, PrecomputedDonorGenotypeSetRow>());
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
    Task<PrecomputedDonorGenotypeSetLookup> GetDonorGenotypeSets(
        IReadOnlyCollection<DonorInput> donors,
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
        int? matchingAlgorithmDataRefreshRecordId) =>
        Task.FromResult(new PrecomputedDonorGenotypeSetLookup(null, null, new Dictionary<int, PrecomputedDonorGenotypeSetRow>()));
}

/// <summary>The default for hosts with no access to the transient matching databases: stores nothing.</summary>
internal class NoOpPrecomputedDonorGenotypeSetWriter : IPrecomputedDonorGenotypeSetWriter
{
    public Task<DonorGenotypeSetStoreResult> Store(
        IReadOnlyCollection<DonorGenotypeSetToStore> genotypeSets,
        IReadOnlySet<Locus> allowedLoci,
        int matchingAlgorithmDataRefreshRecordId) =>
        Task.FromResult(DonorGenotypeSetStoreResult.None);
}
