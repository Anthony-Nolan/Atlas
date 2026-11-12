using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
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

    private readonly IPrecomputedDonorGenotypeSetReader reader;
    private readonly IHaplotypeFrequencyLookupService frequencySetLookup;
    private readonly IAtlasLogger logger;

    public DonorGenotypeSetSourceResolver(
        IPrecomputedDonorGenotypeSetReader reader,
        IHaplotypeFrequencyLookupService frequencySetLookup,
        IAtlasLogger logger)
    {
        this.reader = reader;
        this.frequencySetLookup = frequencySetLookup;
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
            // A stored set's key holds its frequency set, so the set must be chosen before the read.
            var donorsToRead = await WithFrequencySets(donors);
            lookup = await reader.GetDonorGenotypeSets(
                donorsToRead, AllowedLoci(request), request.MatchingAlgorithmDataRefreshRecordId, request.PatientGenotypeSetKey);
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
    /// chooses the value once for each search, when it accepts the search, so a change of its settings does not change
    /// the method of a running search. Empty means live for a call that does not pass through the Atlas Public API too.
    /// </summary>
    /// <remarks>
    /// One exception, while this version is deployed: before it, the match prediction setting <c>Precompute:Mode</c>
    /// could ignore a <c>true</c> on the request (in <c>ForceLive</c>). A search that a client sent with <c>true</c>,
    /// and that is still running when this version is deployed, calculated its first batches live and reads the stored
    /// sets for the rest. Only QA tests set the value, so do not run them during the deploy. The stored sets are checked
    /// against the donor's typing and frequency set before use, so the result is still correct.
    /// </remarks>
    internal static bool UsesPrecomputedGenotypeSets(MatchProbabilityRequestBase request) =>
        request.UsePrecomputedGenotypeSets == true;

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
