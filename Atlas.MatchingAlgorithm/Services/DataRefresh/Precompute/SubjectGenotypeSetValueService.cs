using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Services.Precompute;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;

/// <summary>
/// One genotype set value to find or compute: a typing at one locus combination, imputed against one frequency set.
/// </summary>
/// <param name="SubjectLogDescription">
/// Names the subject in the logs of the conversion step, and nowhere else. For a value that several subjects share, the
/// first request names it.
/// </param>
public sealed record SubjectGenotypeSetValueRequest(
    PhenotypeInfo<string> HlaTyping,
    AllowedLociKey AllowedLociKey,
    HaplotypeFrequencySet FrequencySet,
    string SubjectLogDescription);

/// <summary>Why a value could not be computed.</summary>
public sealed record SubjectGenotypeSetValueFailure(PrecomputeErrorKind Kind, Exception Exception);

/// <summary>What happened to one request.</summary>
/// <param name="ValueId">The id of the stored value, found or computed. Null when there is none.</param>
/// <param name="Failure">
/// Why the value could not be computed. Null when it was computed, and when it was not attempted: the work stopped
/// before it, or its chunk could not be stored.
/// </param>
public sealed record SubjectGenotypeSetValueOutcome(int? ValueId, SubjectGenotypeSetValueFailure Failure)
{
    internal static readonly SubjectGenotypeSetValueOutcome NotAttempted = new(null, null);
}

/// <summary>The outcome of each request, in the order of the requests.</summary>
/// <param name="StoppedBy">
/// The error that stopped the work, or null. A known temporary error of a computation stops it, and so does any error of
/// the database. The requests after it were not attempted.
/// </param>
public sealed record SubjectGenotypeSetValueResults(IReadOnlyList<SubjectGenotypeSetValueOutcome> Outcomes, Exception StoppedBy);

public interface ISubjectGenotypeSetValueService
{
    /// <summary>
    /// The stored value of each request, from <paramref name="targetDatabase"/>: a value that is stored already is found,
    /// and the others are computed and stored. One outcome per request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An error of one computation does not throw.</b> It becomes the outcome of its request. A known permanent error
    /// and an error of unknown cause fail only their own value, and the work goes on. A known temporary error stops the
    /// work, because the values after it would fail too. See <see cref="PrecomputeErrorKind"/>.
    /// </para>
    ///
    /// <para>
    /// <b>The work is kept when it stops.</b> The values that are computed before the stop are stored, so a retry does not
    /// compute them again. An error of the database stops the work too, and then the values of the chunk in hand are lost.
    /// </para>
    ///
    /// <para>
    /// Requests with the same key share one computation, and one outcome.
    /// </para>
    /// </remarks>
    /// <param name="requests">The values to find or compute. The order is the order of the computations.</param>
    /// <param name="matchingAlgorithmHlaNomenclatureVersion">
    /// The nomenclature version of <paramref name="targetDatabase"/>, which the P-group conversion runs at.
    /// </param>
    /// <param name="targetDatabase">The transient database that the values are read from and written to.</param>
    Task<SubjectGenotypeSetValueResults> GetOrComputeValueIds(
        IReadOnlyList<SubjectGenotypeSetValueRequest> requests,
        string matchingAlgorithmHlaNomenclatureVersion,
        TransientDatabase targetDatabase);
}

/// <summary>
/// The value layer of the genotype set precomputation: typings to stored value ids. It computes nothing itself:
/// <see cref="IGenotypeSetService"/> runs expansion, truncation and conversion, and returns what is stored.
/// </summary>
/// <remarks>
/// The data refresh stage computes the values of its groups with this, and gives them to the donors of each group
/// itself. The same steps are in <see cref="SubjectGenotypeSetPrecomputeService"/>, for the donors of a donor import.
/// </remarks>
public class SubjectGenotypeSetValueService : ISubjectGenotypeSetValueService
{
    /// <summary>
    /// Values computed, then stored, per round, so the values of all requests are never held at once. The same as the
    /// repository's <c>StagingChunkSize</c>, so that one store is one staging round trip. A payload is a few KB, and
    /// tens of KB at most, so a round holds a few MB, and tens of MB at most.
    /// </summary>
    internal const int ValueChunkSize = 1000;

    private readonly IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private readonly IGenotypeSetService genotypeSetService;

    public SubjectGenotypeSetValueService(IStaticallyChosenDatabaseRepositoryFactory repositoryFactory, IGenotypeSetService genotypeSetService)
    {
        this.repositoryFactory = repositoryFactory;
        this.genotypeSetService = genotypeSetService;
    }

    /// <inheritdoc />
    public async Task<SubjectGenotypeSetValueResults> GetOrComputeValueIds(
        IReadOnlyList<SubjectGenotypeSetValueRequest> requests,
        string matchingAlgorithmHlaNomenclatureVersion,
        TransientDatabase targetDatabase)
    {
        if (requests == null || requests.Count == 0)
        {
            return new SubjectGenotypeSetValueResults([], null);
        }

        var repository = repositoryFactory.GetSubjectGenotypeSetRepositoryForDatabase(targetDatabase);
        var keyedRequests = requests.Select(WithEmptyHlaNamesAsNull).Select(request => new KeyedRequest(KeyOf(request), request)).ToList();

        var ids = new Dictionary<SubjectGenotypeSetKey, int>();
        var failures = new Dictionary<SubjectGenotypeSetKey, SubjectGenotypeSetValueFailure>();
        Exception stoppedBy = null;

        try
        {
            // A read, not a guarantee: GetOrCreateValueIds checks again under a lock. This only saves the imputations of
            // the values that an earlier batch stored.
            foreach (var (key, id) in await repository.GetExistingValueIds(keyedRequests.Select(request => request.Key).Distinct().ToList()))
            {
                ids[key] = id;
            }

            var toCompute = keyedRequests.Where(request => !ids.ContainsKey(request.Key)).DistinctBy(request => request.Key).ToList();
            foreach (var chunk in toCompute.Chunk(ValueChunkSize))
            {
                var (values, computationStoppedBy) = await Compute(chunk, matchingAlgorithmHlaNomenclatureVersion, failures);
                stoppedBy = computationStoppedBy;

                // Also when the computation stopped: the values that it computed are done.
                foreach (var (key, id) in await repository.GetOrCreateValueIds(values))
                {
                    ids[key] = id;
                }

                if (stoppedBy != null)
                {
                    break;
                }
            }
        }
        catch (Exception exception)
        {
            // The database failed: nothing more can be stored.
            stoppedBy ??= exception;
        }

        var outcomes = keyedRequests
            .Select(request => ids.TryGetValue(request.Key, out var id)
                ? new SubjectGenotypeSetValueOutcome(id, null)
                : failures.TryGetValue(request.Key, out var failure)
                    ? new SubjectGenotypeSetValueOutcome(null, failure)
                    : SubjectGenotypeSetValueOutcome.NotAttempted)
            .ToList();

        return new SubjectGenotypeSetValueResults(outcomes, stoppedBy);
    }

    /// <summary>
    /// Computes the values of a chunk one at a time, and stops at the first known temporary error.
    /// </summary>
    /// <remarks>
    /// One at a time: each call is a full imputation, whose own peak memory is what limits the size of a batch. To run the
    /// imputations of a chunk at the same time is a change to make on purpose, with a measurement.
    /// </remarks>
    private async Task<(List<SubjectGenotypeSetValueToStore> Values, Exception StoppedBy)> Compute(
        IReadOnlyCollection<KeyedRequest> chunk,
        string matchingAlgorithmHlaNomenclatureVersion,
        IDictionary<SubjectGenotypeSetKey, SubjectGenotypeSetValueFailure> failures)
    {
        var values = new List<SubjectGenotypeSetValueToStore>(chunk.Count);

        foreach (var (key, request) in chunk)
        {
            try
            {
                values.Add(await Compute(key, request, matchingAlgorithmHlaNomenclatureVersion));
            }
            catch (Exception exception)
            {
                var kind = PrecomputeErrorClassifier.Classify(exception);
                failures[key] = new SubjectGenotypeSetValueFailure(kind, exception);

                if (kind == PrecomputeErrorKind.KnownTemporary)
                {
                    return (values, exception);
                }
            }
        }

        return (values, null);
    }

    private async Task<SubjectGenotypeSetValueToStore> Compute(
        SubjectGenotypeSetKey key,
        SubjectGenotypeSetValueRequest request,
        string matchingAlgorithmHlaNomenclatureVersion)
    {
        var subjectData = new SubjectData(request.HlaTyping, new SubjectFrequencySet(request.FrequencySet, request.SubjectLogDescription));

        // A copy of the locus set rather than the shared one behind AllowedLociKey.ToLoci(): MatchPredictionParameters
        // exposes it as a mutable ISet.
        var parameters = new MatchPredictionParameters(request.AllowedLociKey.ToLoci().ToHashSet(), matchingAlgorithmHlaNomenclatureVersion);

        var genotypeSet = await genotypeSetService.GetGenotypeSet(subjectData, parameters);

        // NULL for an unrepresented subject is the policy of the precomputation, not of the format: the column is the
        // cheaper place to say "there is nothing here".
        return new SubjectGenotypeSetValueToStore(
            key,
            genotypeSet.IsUnrepresented,
            genotypeSet.IsUnrepresented ? null : SubjectGenotypeSetPayload.Encode(genotypeSet));
    }

    /// <summary>
    /// The request with each empty HLA name changed to null, so the key and the imputation read the same typing. The key
    /// reads null and empty as the same untyped position, but imputation sends an empty name to the HLA Metadata
    /// Dictionary, which throws.
    /// </summary>
    private static SubjectGenotypeSetValueRequest WithEmptyHlaNamesAsNull(SubjectGenotypeSetValueRequest request) =>
        request with { HlaTyping = request.HlaTyping?.Map(hla => string.IsNullOrEmpty(hla) ? null : hla) };

    private static SubjectGenotypeSetKey KeyOf(SubjectGenotypeSetValueRequest request) => new(
        SubjectGenotypeSetKeyGenerator.GenerateHlaTypingKey(request.HlaTyping, request.AllowedLociKey),
        request.FrequencySet.Id,
        request.AllowedLociKey);

    private sealed record KeyedRequest(SubjectGenotypeSetKey Key, SubjectGenotypeSetValueRequest Request);
}
