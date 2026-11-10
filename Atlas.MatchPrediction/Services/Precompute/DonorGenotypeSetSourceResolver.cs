using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;

namespace Atlas.MatchPrediction.Services.Precompute;

public interface IDonorGenotypeSetSourceResolver
{
    /// <summary>
    /// Decides whether a donor batch uses precomputed genotype sets and, if it does, reads the stored rows of all its
    /// donors at once, and the patient's row when the batch has a patient key.
    /// </summary>
    /// <remarks>
    /// Call once per batch, before its donors run, and pass the result down. Not once per donor: the parallel path
    /// gives each donor its own scope, and the read is one SQL round trip for the whole batch.
    /// </remarks>
    Task<DonorGenotypeSetBatchContext> Resolve(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorInput> donors);
}

internal class DonorGenotypeSetSourceResolver : IDonorGenotypeSetSourceResolver
{
    internal const string ReadFailedEventName = "Precomputed genotype set read failed";

    private readonly IPrecomputedDonorGenotypeSetReader reader;
    private readonly IAtlasLogger logger;

    public DonorGenotypeSetSourceResolver(
        IPrecomputedDonorGenotypeSetReader reader,
        IAtlasLogger logger)
    {
        this.reader = reader;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<DonorGenotypeSetBatchContext> Resolve(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorInput> donors)
    {
        if (!UsesPrecomputedGenotypeSets(request))
        {
            // No read at all: when the path is off, nothing precomputed is trusted, not even partly.
            return DonorGenotypeSetBatchContext.Disabled(request.MatchingAlgorithmDataRefreshRecordId, request.PatientGenotypeSetKey);
        }

        PrecomputedDonorGenotypeSetLookup lookup;
        try
        {
            lookup = await reader.GetDonorGenotypeSets(
                donors, AllowedLoci(request), request.MatchingAlgorithmDataRefreshRecordId, request.PatientGenotypeSetKey);
        }
        catch (Exception exception)
        {
            LogReadFailure(request, exception);
            lookup = PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ReadFailed);
        }

        return DonorGenotypeSetBatchContext.Enabled(request.MatchingAlgorithmDataRefreshRecordId, lookup, request.PatientGenotypeSetKey);
    }

    /// <summary>
    /// The request value alone decides: <c>true</c> is stored, <c>false</c> or empty is live. The Atlas Public API
    /// chooses the value once for each search, when it accepts the search, so a running search keeps its method.
    /// Empty means live for a call that does not pass through the Atlas Public API too.
    /// </summary>
    internal static bool UsesPrecomputedGenotypeSets(MatchProbabilityRequestBase request) =>
        request.UsePrecomputedGenotypeSets == true;

    private void LogReadFailure(IdentifiedMatchProbabilityRequest request, Exception exception)
    {
        try
        {
            logger.SendException(exception, LogLevel.Warn, new Dictionary<string, string>
            {
                { "EventName", ReadFailedEventName },
                { "SearchRequestId", request.SearchRequestId },
            });
        }
        catch
        {
            // Logging must not fail the batch.
        }
    }

    /// <summary>The same loci <c>MatchProbabilityService</c> runs match prediction on.</summary>
    internal static IReadOnlySet<Locus> AllowedLoci(MatchProbabilityRequestBase request) =>
        LocusSettings.MatchPredictionLoci.Except(request.ExcludedLoci ?? []).ToHashSet();
}
