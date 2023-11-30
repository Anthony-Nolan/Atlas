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

        public DonorHelper(IMatchingAlgorithmSearchLogger searchLogger, IDonorReader donorReader)
        {
            this.searchLogger = searchLogger;
            this.donorReader = donorReader;
        }

        public async Task<Dictionary<int, DonorLookupInfo>> GetDonorLookup(List<MatchAndScoreResult> reifiedScoredMatches)
        {
            return reifiedScoredMatches.ToDictionary(r => r.MatchResult.DonorId, r => r.MatchResult.DonorInfo.ToDonorLookupInfo());
        }
    }
}
