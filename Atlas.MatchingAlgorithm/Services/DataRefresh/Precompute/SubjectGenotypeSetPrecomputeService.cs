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
/// compute, encode and store the rest a chunk at a time, assign.
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
    /// <summary>
    /// Values computed, then stored, per round - so a batch's values are never all held at once.
    ///
    /// <para>
    /// The same as the repository's <c>StagingChunkSize</c>, so that one store is one staging round trip. Measured
    /// payloads average 3.4 KB and reach 36 KB at the 2,000-genotype cap, so a round holds roughly 3.4 MB and at most
    /// 36 MB, where a whole batch of 8,000 values could hold up to 290 MB.
    /// </para>
    /// </summary>
    internal const int ValueChunkSize = 1000;

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
            .Select(WithEmptyHlaNamesAsNull)
            .SelectMany(subject => AllowedLociKeyExtensions.All.Select(allowedLociKey => new PrecomputeRequest(subject, allowedLociKey)))
            .ToList();

        // A read, not a guarantee. Nothing here depends on the answer still being true by the time the write runs -
        // GetOrCreateValueIds re-checks under lock - so this is only ever about not paying for an imputation twice.
        // Do not "simplify" it into a blind insert on the strength of what it returned.
        var storedIds = await repository.GetExistingValueIds(requests.Select(request => request.Key).Distinct().ToList());

        // Values before assignments, never the other way round. A crash between the two leaves value rows that no
        // donor points at, which the next run finds and reuses; the opposite order would leave donor rows pointing at
        // ids that do not exist, and there is no foreign key on the transient databases to catch that.
        var createdIds = await ComputeAndStore(
            requests.Where(request => !storedIds.ContainsKey(request.Key)).DistinctBy(request => request.Key).ToList(),
            matchingAlgorithmHlaNomenclatureVersion);

        await repository.WriteDonorAssignments(requests
            .Select(request => new DonorSubjectGenotypeSetAssignment(
                request.Subject.DonorId,
                request.AllowedLociKey,
                storedIds.TryGetValue(request.Key, out var storedId) ? storedId : createdIds[request.Key]))
            .ToList());
    }

    /// <summary>
    /// <paramref name="subject"/> with each empty HLA name changed to null. The key and the imputation then read the same
    /// typing.
    /// </summary>
    /// <remarks>
    /// The matching algorithm reads null and empty as the same thing, an untyped position (see <c>DonorHlaExpander</c>).
    /// The key does too: neither adds anything to the canonical string. Imputation does not. <c>CompressedPhenotypeConverter</c>
    /// skips only null, and sends an empty name to the HLA Metadata Dictionary, which throws. Without this step, a donor
    /// with an empty name fails its whole batch - unless a donor with the same key and a null name was imputed first.
    /// Whether a batch fails would then depend on donor order.
    /// </remarks>
    private static PrecomputeSubject WithEmptyHlaNamesAsNull(PrecomputeSubject subject) =>
        subject with { HlaTyping = subject.HlaTyping?.Map(hla => string.IsNullOrEmpty(hla) ? null : hla) };

    /// <summary>
    /// Computes the values <see cref="ValueChunkSize"/> at a time, and stores each chunk before the next is computed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Memory.</b> Only one chunk of payloads is held at a time, not every payload in the batch.
    /// </para>
    ///
    /// <para>
    /// <b>Loss.</b> A batch is thousands of imputations of about 100 ms each. A crash loses only the chunk in hand: the
    /// stored chunks are found by the next run's <c>GetExistingValueIds</c>, and are not computed again.
    /// </para>
    ///
    /// <para>
    /// <b>Sequential, not a producer and a consumer.</b> A store is one staging round trip of about 3.4 MB, and the
    /// chunk it stores took about 100 seconds of imputation to compute. Running the two side by side could only hide
    /// the store - a small part of that time - at the cost of a channel, error propagation and cancellation.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> ComputeAndStore(
        IReadOnlyCollection<PrecomputeRequest> requests,
        string matchingAlgorithmHlaNomenclatureVersion)
    {
        var ids = new Dictionary<SubjectGenotypeSetKey, int>(requests.Count);

        foreach (var chunk in requests.Chunk(ValueChunkSize))
        {
            var values = await Compute(chunk, matchingAlgorithmHlaNomenclatureVersion);

            foreach (var (key, id) in await repository.GetOrCreateValueIds(values))
            {
                ids[key] = id;
            }
        }

        return ids;
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

            // TODO: ATL-233/ATL-314: one throw here fails the whole batch, not only this donor; the chunks already stored
            // are kept. Let a single donor fail on its own (the stage-55 PermanentlyFailed status and graceful
            // degradation) when the service is wired in.
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
