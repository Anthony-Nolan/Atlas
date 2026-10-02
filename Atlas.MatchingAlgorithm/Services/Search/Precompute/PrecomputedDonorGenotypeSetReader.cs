using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchPrediction.Services.Precompute;

namespace Atlas.MatchingAlgorithm.Services.Search.Precompute;

/// <summary>
/// Reads the precomputed genotype sets of a match prediction batch's donors from the transient database that matching
/// used - and only while that database is still the active one (ATL-221, ATL-433).
/// </summary>
/// <remarks>
/// <para>
/// <b>Pinned, not "the active database now".</b> A data refresh can swap the active database between matching and
/// match prediction. The new database can be at a different HLA nomenclature version, so its stored P groups would not
/// fit the search. So the reader uses the database of the record matching ran against, and only while that record
/// is still the latest successful one.
/// </para>
///
/// <para>
/// <b>Never the dormant database.</b> After a swap, the old database is scaled down, can be auto-paused, and is wiped by
/// the next refresh. Reading it could wake it, and its rows can vanish. So after a swap the batch is computed live.
/// </para>
///
/// <para>
/// The active record is read from <see cref="IDataRefreshHistoryRepository"/> on every call, not through the
/// scope-cached <c>IActiveDataRefreshRecordAccessor</c>, so each batch sees a swap that happened during the search.
/// </para>
/// </remarks>
public class PrecomputedDonorGenotypeSetReader : IPrecomputedDonorGenotypeSetReader
{
    private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private readonly ISubjectGenotypeSetRepositoryFactory repositoryFactory;

    public PrecomputedDonorGenotypeSetReader(
        IDataRefreshHistoryRepository dataRefreshHistoryRepository,
        ISubjectGenotypeSetRepositoryFactory repositoryFactory)
    {
        this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        this.repositoryFactory = repositoryFactory;
    }

    /// <inheritdoc />
    public async Task<PrecomputedDonorGenotypeSetLookup> GetDonorGenotypeSets(
        IReadOnlyCollection<int> donorIds,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId)
    {
        // The search validator should prevent any other locus set, but an uncovered set must be computed live, not fail.
        if (!AllowedLociKeyExtensions.TryToAllowedLociKey(allowedLoci, out var allowedLociKey))
        {
            return PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.UncoveredAllowedLoci);
        }

        if (matchingAlgorithmDataRefreshRecordId == null)
        {
            return PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseUnknown, allowedLociKey.ToString());
        }

        var activeRecord = dataRefreshHistoryRepository.GetActiveRecord();
        if (activeRecord == null || activeRecord.Id != matchingAlgorithmDataRefreshRecordId)
        {
            return PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ActiveDatabaseChanged, allowedLociKey.ToString());
        }

        var stored = await repositoryFactory
            .GetForDatabase(activeRecord.Database)
            .GetDonorSubjectGenotypeSets(donorIds, allowedLociKey);

        return new PrecomputedDonorGenotypeSetLookup(
            allowedLociKey.ToString(),
            null,
            stored.ToDictionary(
                s => s.Key,
                s => new PrecomputedDonorGenotypeSetRow(s.Value.HaplotypeFrequencySetId, s.Value.IsUnrepresented, s.Value.SubjectGenotypeSetData)));
    }
}
