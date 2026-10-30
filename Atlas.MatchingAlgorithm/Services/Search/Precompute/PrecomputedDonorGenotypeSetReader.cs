using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;

namespace Atlas.MatchingAlgorithm.Services.Search.Precompute;

/// <summary>
/// Reads the precomputed genotype sets of a match prediction batch's donors (ATL-221), and of the search's patient
/// (ATL-426), from the transient database that matching used - and only while that database is still the active one
/// (ATL-433).
/// </summary>
/// <remarks>
/// <para>
/// <b>By key, not through the donors' link rows</b> (ATL-424). A value is fully defined by its key: the typing, the
/// frequency set and the loci. Search has all three for each donor input, so it reads each distinct key of the batch
/// once, and every id of the input uses that value. A link row follows the donor's current typing, which an import can
/// change after matching; the key follows the typing that matching read.
/// </para>
///
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
        IReadOnlyCollection<DonorGenotypeSetToRead> donors,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId,
        PatientGenotypeSetKey patientGenotypeSetKey)
    {
        var unavailableReason = Pin(allowedLoci, matchingAlgorithmDataRefreshRecordId, out var allowedLociKey, out var activeRecord);
        if (unavailableReason != null)
        {
            return PrecomputedDonorGenotypeSetLookup.Unavailable(
                unavailableReason.Value,
                unavailableReason == DonorGenotypeSetSource.UncoveredAllowedLoci ? null : allowedLociKey.ToString());
        }

        // The key of each donor input, for every id in it. A donor id in two inputs keeps the key of the first.
        var keyByDonorId = new Dictionary<int, SubjectGenotypeSetKey>();
        foreach (var donor in donors)
        {
            var key = new SubjectGenotypeSetKey(
                SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(donor.Donor.DonorHla.ToPhenotypeInfo(), allowedLociKey),
                donor.HaplotypeFrequencySetId,
                allowedLociKey);

            foreach (var donorId in donor.Donor.DonorIds)
            {
                keyByDonorId.TryAdd(donorId, key);
            }
        }

        // One read for the batch, of each distinct key once: inputs with the same key share it, and so does the patient
        // when its key is a donor input's.
        var patientKey = PatientKey(patientGenotypeSetKey, allowedLociKey);
        var keys = keyByDonorId.Values.ToHashSet();
        if (patientKey != null)
        {
            keys.Add(patientKey.Value);
        }

        var values = await repositoryFactory.GetForDatabase(activeRecord.Database).GetSubjectGenotypeSetValues(keys);

        var rowByKey = values.ToDictionary(
            value => value.Key,
            value => new PrecomputedDonorGenotypeSetRow(
                value.Key.HaplotypeFrequencySetId, value.Value.IsUnrepresented, value.Value.SubjectGenotypeSetData));

        return new PrecomputedDonorGenotypeSetLookup(
            allowedLociKey.ToString(),
            null,
            keyByDonorId
                .Where(donorKey => rowByKey.ContainsKey(donorKey.Value))
                .ToDictionary(donorKey => donorKey.Key, donorKey => rowByKey[donorKey.Value]),
            patientKey != null && values.TryGetValue(patientKey.Value, out var patientValue)
                ? new PrecomputedPatientGenotypeSetRow(patientValue.IsUnrepresented, patientValue.SubjectGenotypeSetData)
                : null);
    }

    /// <inheritdoc />
    public async Task<PatientGenotypeSetLookup> FindPatientGenotypeSet(
        PhenotypeInfo<string> patientHla,
        int haplotypeFrequencySetId,
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId)
    {
        var unavailableReason = Pin(allowedLoci, matchingAlgorithmDataRefreshRecordId, out var allowedLociKey, out var activeRecord);
        if (unavailableReason != null)
        {
            return PatientGenotypeSetLookup.Unavailable(unavailableReason.Value);
        }

        // The key generator treats null and empty names the same, so the patient's key matches a donor's for the same typing.
        var key = new SubjectGenotypeSetKey(
            SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(patientHla, allowedLociKey),
            haplotypeFrequencySetId,
            allowedLociKey);

        var existing = await repositoryFactory.GetForDatabase(activeRecord.Database).GetExistingValueIds([key]);

        return new PatientGenotypeSetLookup(
            new PatientGenotypeSetKey(key.HlaTypingKey, key.HaplotypeFrequencySetId, key.AllowedLociKey.ToString()),
            existing.ContainsKey(key),
            null);
    }

    /// <summary>
    /// The checks every read starts with: the loci map to a precomputed key, and the record matching used is known and
    /// still active. Returns null when they pass, and the reason otherwise.
    /// </summary>
    private DonorGenotypeSetSource? Pin(
        IReadOnlySet<Locus> allowedLoci,
        int? matchingAlgorithmDataRefreshRecordId,
        out AllowedLociKey allowedLociKey,
        out ActiveDataRefreshRecord activeRecord)
    {
        activeRecord = null;

        // The search validator should prevent any other locus set, but an uncovered set must be computed live, not fail.
        if (!AllowedLociKeyExtensions.TryToAllowedLociKey(allowedLoci, out allowedLociKey))
        {
            return DonorGenotypeSetSource.UncoveredAllowedLoci;
        }

        if (matchingAlgorithmDataRefreshRecordId == null)
        {
            return DonorGenotypeSetSource.ActiveDatabaseUnknown;
        }

        activeRecord = dataRefreshHistoryRepository.GetActiveRecord();
        if (activeRecord == null || activeRecord.Id != matchingAlgorithmDataRefreshRecordId)
        {
            return DonorGenotypeSetSource.ActiveDatabaseChanged;
        }

        return null;
    }

    /// <summary>
    /// The key of the patient's stored row, read with the donors' rows under the same pinning. Null when the batch has no
    /// patient key, or a key for other loci (not possible within one search): the patient is then computed live.
    /// </summary>
    private static SubjectGenotypeSetKey? PatientKey(PatientGenotypeSetKey patientGenotypeSetKey, AllowedLociKey allowedLociKey) =>
        patientGenotypeSetKey == null || patientGenotypeSetKey.AllowedLociKey != allowedLociKey.ToString()
            ? null
            : new SubjectGenotypeSetKey(patientGenotypeSetKey.HlaTypingKey, patientGenotypeSetKey.HaplotypeFrequencySetId, allowedLociKey);
}
