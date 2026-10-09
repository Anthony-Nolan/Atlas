using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;

namespace Atlas.MatchingAlgorithm.Services.Search.Precompute;

/// <summary>
/// Stores the donor genotype sets that search computed live, so the next search does not compute them again (ATL-221).
/// </summary>
/// <remarks>
/// <para>
/// A donor gets here when it had no row, or a row for a since-replaced frequency set. Today only the differential
/// donor import (ATL-232) precomputes rows, so this covers donors it has not reached yet and donors whose pre-computation
/// failed; the Data Refresh stage (ATL-233) is planned. The set stored here is exactly what the precompute would have
/// stored - the same genotype set service, the same frequency set lookup, the same nomenclature version (that of the
/// pinned database) and the same key - so a row written here and a row written by the precompute are the same row.
/// </para>
///
/// <para>
/// <b>Two guards.</b> Nothing is written unless the database matching used is still active, checked again here just
/// before the write; the dormant database is never written, so a refresh that is filling it is not affected. And an
/// assignment is written only while the donor's typing, registry code and ethnicity code are still the ones the set was
/// computed from (see <see cref="ISubjectGenotypeSetRepository.UpsertDonorAssignmentsWhereDonorUnchanged"/>).
/// </para>
///
/// <para>
/// Throws on failure: the caller treats the store as best effort and logs it.
/// </para>
/// </remarks>
public class PrecomputedDonorGenotypeSetWriter : IPrecomputedDonorGenotypeSetWriter
{
    private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
    private readonly ISubjectGenotypeSetRepositoryFactory repositoryFactory;

    public PrecomputedDonorGenotypeSetWriter(
        IDataRefreshHistoryRepository dataRefreshHistoryRepository,
        ISubjectGenotypeSetRepositoryFactory repositoryFactory)
    {
        this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        this.repositoryFactory = repositoryFactory;
    }

    /// <inheritdoc />
    public async Task<DonorGenotypeSetStoreResult> Store(
        IReadOnlyCollection<DonorGenotypeSetToStore> genotypeSets,
        IReadOnlySet<Locus> allowedLoci,
        int matchingAlgorithmDataRefreshRecordId)
    {
        if (genotypeSets == null || genotypeSets.Count == 0 || !AllowedLociKeyExtensions.TryToAllowedLociKey(allowedLoci, out var allowedLociKey))
        {
            return DonorGenotypeSetStoreResult.None;
        }

        var activeRecord = dataRefreshHistoryRepository.GetActiveRecord();
        if (activeRecord == null || activeRecord.Id != matchingAlgorithmDataRefreshRecordId)
        {
            return new DonorGenotypeSetStoreResult(0, 0, genotypeSets.Sum(s => s.DonorIds.Count));
        }

        var repository = repositoryFactory.GetForDatabase(activeRecord.Database);

        var toStore = genotypeSets
            .Select(s => (Set: s, Key: new SubjectGenotypeSetKey(
                SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(WithEmptyHlaNamesAsNull(s.DonorHla), allowedLociKey),
                s.HaplotypeFrequencySetId,
                allowedLociKey)))
            .ToList();

        // Values first, outside the guarded transaction: a value row is content-keyed and shared, so one that ends up
        // with no assignment (a skipped donor, or a failure before the upsert) is harmless and is reused by the next
        // writer of the same key.
        var valueIds = await repository.GetOrCreateValueIds(toStore
            .Select(s => new SubjectGenotypeSetValueToStore(s.Key, s.Set.IsUnrepresented, s.Set.SubjectGenotypeSetData))
            .ToList());

        var upsertResult = await repository.UpsertDonorAssignmentsWhereDonorUnchanged(toStore
            .SelectMany(s => s.Set.DonorIds.Select(donorId => new GuardedDonorAssignment(
                new DonorSubjectGenotypeSetAssignment(donorId, allowedLociKey, valueIds[s.Key]),
                s.Set.DonorHla,
                s.Set.RegistryCode,
                s.Set.EthnicityCode)))
            .ToList());

        return new DonorGenotypeSetStoreResult(upsertResult.UpsertedCount, upsertResult.SkippedDonorChangedCount, 0);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// A value row only, through the same concurrency-safe get-or-create as every other writer. No guard is needed: the
    /// row is content-keyed and no donor points at it, so it cannot make any donor use a wrong set. A donor or a later
    /// search with the same key reuses it.
    /// </para>
    ///
    /// <para>
    /// The record is checked again just before the write, as for donors. If the database is swapped after that
    /// check, the row lands in the database matching used, computed with that database's nomenclature version - so it
    /// is still right for that database, which the next refresh then wipes. No clean-up is needed.
    /// </para>
    /// </remarks>
    public async Task<bool> StorePatientGenotypeSet(
        PatientGenotypeSetKey key,
        bool isUnrepresented,
        byte[] subjectGenotypeSetData,
        int matchingAlgorithmDataRefreshRecordId)
    {
        var activeRecord = dataRefreshHistoryRepository.GetActiveRecord();
        if (activeRecord == null || activeRecord.Id != matchingAlgorithmDataRefreshRecordId)
        {
            return false;
        }

        var valueKey = new SubjectGenotypeSetKey(key.HlaTypingKey, key.HaplotypeFrequencySetId, Enum.Parse<AllowedLociKey>(key.AllowedLociKey));
        await repositoryFactory
            .GetForDatabase(activeRecord.Database)
            .GetOrCreateValueIds([new SubjectGenotypeSetValueToStore(valueKey, isUnrepresented, subjectGenotypeSetData)]);

        return true;
    }

    /// <summary>
    /// The same normalisation the data refresh precompute applies before it builds a key, so both write the same key
    /// for the same typing. See <c>SubjectGenotypeSetPrecomputeService.WithEmptyHlaNamesAsNull</c>.
    /// </summary>
    private static PhenotypeInfo<string> WithEmptyHlaNamesAsNull(PhenotypeInfo<string> hla) =>
        hla?.Map(name => string.IsNullOrEmpty(name) ? null : name);
}
