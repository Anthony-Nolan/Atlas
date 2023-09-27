using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.FeatureManagement;
using Atlas.DonorImport.ExternalInterface;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Config;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Services.Donors
{
    public interface IDonorHelper
    {
        Task<Dictionary<int, DonorLookupInfo>> GetDonorLookup(List<MatchAndScoreResult> reifiedScoredMatches);
    }

    public class DonorHelper : IDonorHelper
    {
        private readonly ILogger searchLogger;
        private readonly IDonorReader donorReader;
        private readonly IAtlasFeatureManager featureManager;

        public DonorHelper(IMatchingAlgorithmSearchLogger searchLogger, IDonorReader donorReader, IAtlasFeatureManager featureManager)
        {
            this.searchLogger = searchLogger;
            this.donorReader = donorReader;
            this.featureManager = featureManager;
        }

        public async Task<Dictionary<int, DonorLookupInfo>> GetDonorLookup(List<MatchAndScoreResult> reifiedScoredMatches)
        {
            var isFeatureEnabled = await featureManager.IsFeatureEnabled(FeatureFlags.UseDonorInfoStoredInMatchingAlgorithmDb);
            searchLogger.SendTrace($"Feature flag {FeatureFlags.UseDonorInfoStoredInMatchingAlgorithmDb} = {isFeatureEnabled}");

            return isFeatureEnabled
                ? GetDonorLookupFromMatchResults(reifiedScoredMatches)
                : await LoadDonorLookupFromDonorStore(reifiedScoredMatches);
        }

        private async Task<Dictionary<int, DonorLookupInfo>> LoadDonorLookupFromDonorStore(List<MatchAndScoreResult> reifiedScoredMatches)
        {
            using var donorLookupTimer = searchLogger.RunTimed($"Matching Algorithm: Look up external donor ids");
            return (await donorReader.GetDonors(reifiedScoredMatches.Select(r => r.MatchResult.DonorId)))
                .ToDictionary(l => l.Key, l => l.Value.ToDonorLookupInfo());
        }

        private Dictionary<int, DonorLookupInfo> GetDonorLookupFromMatchResults(List<MatchAndScoreResult> reifiedScoredMatches)
            => reifiedScoredMatches.ToDictionary(r => r.MatchResult.DonorId, r => r.MatchResult.DonorInfo.ToDonorLookupInfo());
    }
}
