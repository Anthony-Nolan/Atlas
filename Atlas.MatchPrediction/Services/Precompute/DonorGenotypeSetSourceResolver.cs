using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;

namespace Atlas.MatchPrediction.Services.Precompute;

public interface IDonorGenotypeSetSourceResolver
{
    /// <summary>
    /// Decides whether a donor batch uses precomputed genotype sets and, if it does, reads the stored rows of all its
    /// donors at once.
    /// </summary>
    /// <remarks>
    /// Call once per batch, before its donors run, and pass the result down. Not once per donor: the parallel path
    /// gives each donor its own scope, and the read is one SQL round trip for the whole batch.
    /// </remarks>
    Task<DonorGenotypeSetBatchContext> Resolve(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<int> donorIds);
}

internal class DonorGenotypeSetSourceResolver : IDonorGenotypeSetSourceResolver
{
    internal const string ReadFailedEventName = "Precomputed genotype set read failed";

    private readonly PrecomputedGenotypeSetSettings settings;
    private readonly IPrecomputedDonorGenotypeSetReader reader;
    private readonly IAtlasLogger logger;

    public DonorGenotypeSetSourceResolver(
        PrecomputedGenotypeSetSettings settings,
        IPrecomputedDonorGenotypeSetReader reader,
        IAtlasLogger logger)
    {
        this.settings = settings;
        this.reader = reader;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<DonorGenotypeSetBatchContext> Resolve(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<int> donorIds)
    {
        // Resolved here, per batch, and not when the search is submitted, so that turning the kill-switch off also
        // affects a search that is already running and leaves the override null. The request wins in both directions.
        var source = request.UsePrecomputedGenotypeSets.HasValue
            ? UsePrecomputedGenotypeSetsSource.Request
            : UsePrecomputedGenotypeSetsSource.FeatureFlag;
        var usePrecomputedGenotypeSets = request.UsePrecomputedGenotypeSets ?? settings.UsePrecomputedGenotypeSets;

        if (!usePrecomputedGenotypeSets)
        {
            // No read at all: when the path is off, nothing precomputed is trusted, not even partly.
            return DonorGenotypeSetBatchContext.Disabled(source, request.MatchingAlgorithmDataRefreshRecordId);
        }

        PrecomputedDonorGenotypeSetLookup lookup;
        try
        {
            lookup = await reader.GetDonorGenotypeSets(donorIds, AllowedLoci(request), request.MatchingAlgorithmDataRefreshRecordId);
        }
        catch (Exception exception)
        {
            LogReadFailure(request, exception);
            lookup = PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ReadFailed);
        }

        return DonorGenotypeSetBatchContext.Enabled(source, request.MatchingAlgorithmDataRefreshRecordId, lookup);
    }

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
