using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;

namespace Atlas.MatchPrediction.Services.Precompute;

public interface IDonorGenotypeSetSourceResolver
{
    /// <summary>
    /// Decides whether a donor batch uses precomputed genotype sets and, if it does, chooses the frequency set of each
    /// donor input and reads the stored rows of all its donor inputs at once, and the patient's row when the batch has a
    /// patient key.
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

    private readonly PrecomputedGenotypeSetSettings settings;
    private readonly IPrecomputedDonorGenotypeSetReader reader;
    private readonly IHaplotypeFrequencyLookupService frequencySetLookup;
    private readonly IAtlasLogger logger;

    public DonorGenotypeSetSourceResolver(
        PrecomputedGenotypeSetSettings settings,
        IPrecomputedDonorGenotypeSetReader reader,
        IHaplotypeFrequencyLookupService frequencySetLookup,
        IAtlasLogger logger)
    {
        this.settings = settings;
        this.reader = reader;
        this.frequencySetLookup = frequencySetLookup;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<DonorGenotypeSetBatchContext> Resolve(IdentifiedMatchProbabilityRequest request, IReadOnlyCollection<DonorInput> donors)
    {
        // Resolved here, per batch, and not when the search is submitted, so that changing the kill-switch also affects
        // a search that is already running.
        var (usePrecomputedGenotypeSets, source) = Decide(settings.Mode, request.UsePrecomputedGenotypeSets);

        if (!usePrecomputedGenotypeSets)
        {
            // No read at all: when the path is off, nothing precomputed is trusted, not even partly.
            return DonorGenotypeSetBatchContext.Disabled(
                source, settings.Mode, request.MatchingAlgorithmDataRefreshRecordId, request.PatientGenotypeSetKey);
        }

        PrecomputedDonorGenotypeSetLookup lookup;
        IReadOnlyDictionary<string, double> readSteps;
        var readStopwatch = Stopwatch.StartNew();
        // DEV timing only (ATL-531): the reader and the repository add the time of each step of the read to this scope.
        using (ReadStepTimings.Begin(out readSteps))
        {
            try
            {
                // A stored set's key holds its frequency set, so the set must be chosen before the read.
                var frequencySetsStopwatch = Stopwatch.StartNew();
                var donorsToRead = await WithFrequencySets(donors);
                ReadStepTimings.Add("ReadFrequencySetsMs", frequencySetsStopwatch);

                lookup = await reader.GetDonorGenotypeSets(
                    donorsToRead, AllowedLoci(request), request.MatchingAlgorithmDataRefreshRecordId, request.PatientGenotypeSetKey);
            }
            catch (Exception exception)
            {
                LogReadFailure(request, exception);
                lookup = PrecomputedDonorGenotypeSetLookup.Unavailable(DonorGenotypeSetSource.ReadFailed);
            }
        }

        return DonorGenotypeSetBatchContext.Enabled(
            source, settings.Mode, request.MatchingAlgorithmDataRefreshRecordId, lookup, request.PatientGenotypeSetKey, readStopwatch.Elapsed,
            readSteps);
    }

    /// <summary>
    /// <see cref="PrecomputedGenotypeSetMode.ForceLive"/> is the hard off and ignores the request. In the other two
    /// modes, a request that sets the override wins, in either direction; a request that leaves it null gets the mode's
    /// default.
    /// </summary>
    internal static (bool UsePrecomputedGenotypeSets, UsePrecomputedGenotypeSetsSource Source) Decide(
        PrecomputedGenotypeSetMode mode,
        bool? requestOverride) =>
        mode switch
        {
            PrecomputedGenotypeSetMode.ForceLive => (false, UsePrecomputedGenotypeSetsSource.FeatureFlag),
            _ when requestOverride.HasValue => (requestOverride.Value, UsePrecomputedGenotypeSetsSource.Request),
            PrecomputedGenotypeSetMode.DefaultPrecomputed => (true, UsePrecomputedGenotypeSetsSource.FeatureFlag),
            _ => (false, UsePrecomputedGenotypeSetsSource.FeatureFlag)
        };

    /// <summary>
    /// Each donor input with the frequency set that the live path chooses for it (<c>MatchProbabilityService</c>), with
    /// the same lookup. One lookup for each registry and ethnicity: the batch's inputs are sorted by them (ATL-476), and
    /// each lookup reads the cached list of active sets.
    /// </summary>
    private async Task<IReadOnlyCollection<DonorGenotypeSetToRead>> WithFrequencySets(IReadOnlyCollection<DonorInput> donors)
    {
        var setIdsByCodes = new Dictionary<(string RegistryCode, string EthnicityCode), int>();
        var donorsToRead = new List<DonorGenotypeSetToRead>(donors.Count);
        foreach (var donor in donors)
        {
            // As HaplotypeFrequencyLookupService.GetHaplotypeFrequencySets does for the live path.
            var metadata = donor.DonorFrequencySetMetadata ?? new FrequencySetMetadata();
            var codes = (metadata.RegistryCode, metadata.EthnicityCode);
            if (!setIdsByCodes.TryGetValue(codes, out var setId))
            {
                setId = (await frequencySetLookup.GetSingleHaplotypeFrequencySet(metadata)).Id;
                setIdsByCodes[codes] = setId;
            }

            donorsToRead.Add(new DonorGenotypeSetToRead(donor, setId));
        }

        return donorsToRead;
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
