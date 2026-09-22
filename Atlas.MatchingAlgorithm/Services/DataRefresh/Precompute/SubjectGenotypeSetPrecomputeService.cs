using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// One donor to precompute: the typing to impute, and the haplotype frequency set to impute it against.
/// </summary>
/// <remarks>
/// The frequency set arrives resolved, so choosing it - from the donor's registry and ethnicity - stays with the
/// caller, which is where the donor batch and its <c>RegistryCode</c> / <c>EthnicityCode</c> already are.
/// </remarks>
public sealed record PrecomputeSubject(int DonorId, PhenotypeInfo<string> HlaTyping, HaplotypeFrequencySet FrequencySet);

public interface ISubjectGenotypeSetPrecomputeService
{
    /// <summary>
    /// Computes, stores and assigns the genotype set of every given donor at all four allowed-loci combinations.
    /// </summary>
    /// <remarks>
    /// Batch-shaped because its caller is: the data refresh's HLA processing works in batches of 2,000 donors, so one
    /// call covers 2,000 x 4 = 8,000 rows. That is also what makes de-duplication worth anything - two donors with the
    /// same typing only ever pay for one imputation.
    /// </remarks>
    Task Precompute(IReadOnlyCollection<PrecomputeSubject> subjects, string matchingAlgorithmHlaNomenclatureVersion);
}

/// <summary>
/// Fills <c>SubjectGenotypeSetValues</c> and <c>DonorSubjectGenotypeSets</c>, so that a search does not have to impute
/// a donor's genotype set it has already imputed.
///
/// <para>
/// <b>It computes nothing itself.</b> <see cref="IGenotypeSetService"/> already runs expand, truncate and convert and
/// returns exactly the type that gets stored, so this class is orchestration: derive the key, skip what is stored,
/// compute the rest, encode, store, assign.
/// </para>
///
/// <para>
/// <b>Four imputations per donor.</b> The four combinations are nested, but their results are not derivable from one
/// another - every stage of imputation gates on the allowed loci, so restricting the locus set changes the answer
/// rather than trimming it. This is therefore four times the most expensive operation the system performs, per donor.
/// </para>
/// </summary>
public class SubjectGenotypeSetPrecomputeService : ISubjectGenotypeSetPrecomputeService
{
    private readonly ISubjectGenotypeSetRepository repository;
    private readonly IGenotypeSetService genotypeSetService;

    public SubjectGenotypeSetPrecomputeService(IDormantRepositoryFactory repositoryFactory, IGenotypeSetService genotypeSetService)
    {
        repository = repositoryFactory.GetSubjectGenotypeSetRepository();
        this.genotypeSetService = genotypeSetService;
    }

    /// <inheritdoc />
    public async Task Precompute(IReadOnlyCollection<PrecomputeSubject> subjects, string matchingAlgorithmHlaNomenclatureVersion)
    {
        if (subjects == null || subjects.Count == 0)
        {
            return;
        }

        var requests = subjects
            .SelectMany(subject => AllowedLociKeyExtensions.All.Select(allowedLociKey => new PrecomputeRequest(subject, allowedLociKey)))
            .ToList();

        // A read, not a guarantee. Nothing here depends on the answer still being true by the time the write runs -
        // GetOrCreateValueIds re-checks under lock - so this is only ever about not paying for an imputation twice.
        // Do not "simplify" it into a blind insert on the strength of what it returned.
        var storedIds = await repository.GetExistingValueIds(requests.Select(request => request.Key).Distinct().ToList());

        var computedValues = await Compute(
            requests.Where(request => !storedIds.ContainsKey(request.Key)).DistinctBy(request => request.Key).ToList(),
            matchingAlgorithmHlaNomenclatureVersion);

        // Values before assignments, never the other way round. A crash between the two leaves value rows that no
        // donor points at, which the next run finds and reuses; the opposite order would leave donor rows pointing at
        // ids that do not exist, and there is no foreign key on the transient databases to catch that.
        var createdIds = await repository.GetOrCreateValueIds(computedValues);

        await repository.WriteDonorAssignments(requests
            .Select(request => new DonorSubjectGenotypeSetAssignment(
                request.Subject.DonorId,
                request.AllowedLociKey,
                storedIds.TryGetValue(request.Key, out var storedId) ? storedId : createdIds[request.Key]))
            .ToList());
    }

    /// <remarks>
    /// One at a time: each call is a full imputation, whose own peak memory is what caps the donor batch size in the
    /// first place. Running the batch's imputations concurrently is a change to make deliberately, with a measurement,
    /// not a detail of this loop.
    /// </remarks>
    private async Task<IReadOnlyCollection<SubjectGenotypeSetValueToStore>> Compute(
        IReadOnlyCollection<PrecomputeRequest> requests,
        string matchingAlgorithmHlaNomenclatureVersion)
    {
        var values = new List<SubjectGenotypeSetValueToStore>(requests.Count);

        foreach (var request in requests)
        {
            var subjectData = new SubjectData(
                request.Subject.HlaTyping,
                // The donor named here is whichever one of the donors sharing this key was reached first, since the
                // result is shared by all of them. It reaches the logs of the conversion step, and nothing else.
                new SubjectFrequencySet(request.Subject.FrequencySet, $"donor {request.Subject.DonorId}"));

            // A copy of the locus set rather than the shared one behind AllowedLociKey.ToLoci(): MatchPredictionParameters
            // exposes it as a mutable ISet. The allocation is nothing against the imputation that follows it.
            var parameters = new MatchPredictionParameters(
                request.AllowedLociKey.ToLoci().ToHashSet(),
                matchingAlgorithmHlaNomenclatureVersion);

            var genotypeSet = await genotypeSetService.GetGenotypeSet(subjectData, parameters);

            // Storing NULL for an unrepresented subject is this class's policy, not the format's - the encoder round
            // trips an unrepresented set perfectly well. The column is the cheaper place to say "there is nothing here".
            values.Add(new SubjectGenotypeSetValueToStore(
                request.Key,
                genotypeSet.IsUnrepresented,
                genotypeSet.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(genotypeSet)));
        }

        return values;
    }

    /// <summary>One donor at one locus combination, with the key that combination resolves to.</summary>
    private sealed record PrecomputeRequest
    {
        internal PrecomputeSubject Subject { get; }
        internal AllowedLociKey AllowedLociKey { get; }
        internal SubjectGenotypeSetKey Key { get; }

        internal PrecomputeRequest(PrecomputeSubject subject, AllowedLociKey allowedLociKey)
        {
            Subject = subject;
            AllowedLociKey = allowedLociKey;
            Key = new SubjectGenotypeSetKey(
                SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(subject.HlaTyping, allowedLociKey),
                subject.FrequencySet.Id,
                allowedLociKey);
        }
    }
}
