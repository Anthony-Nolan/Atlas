using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.DurableFunctions.Search.Activity
{
    public class SearchActivityFunctions
    {
        private readonly ISearchRunner searchRunner;
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        public SearchActivityFunctions(ISearchRunner searchRunner, IMatchPredictionAlgorithm matchPredictionAlgorithm)
        {
            this.searchRunner = searchRunner;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
        }
        
        [FunctionName(nameof(RunMatchingAlgorithm))]
        public async Task<SearchResultSet> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            return await searchRunner.RunSearch(searchRequest);
        }
        
        [FunctionName(nameof(RunMatchPrediction))]
        public async Task<MatchProbabilityResponse> RunMatchPrediction([ActivityTrigger] MatchProbabilityInput matchProbabilityInput)
        {
            return await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(matchProbabilityInput);
        }
    }
}