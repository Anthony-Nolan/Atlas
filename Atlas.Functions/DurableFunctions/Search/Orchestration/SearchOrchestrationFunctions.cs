using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Atlas.Functions.DurableFunctions.Search.Orchestration
{
    /// <summary>
    /// Note that orchestration triggered functions will be run multiple times per-request scope, so need to be completely deterministic.
    /// Any expensive or non-deterministic code should be called from an Activity function.
    /// </summary>
    public static class SearchOrchestrationFunctions
    {
        [FunctionName(nameof(SearchOrchestrator))]
        public static async Task<List<string>> SearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var searchRequest = context.GetInput<SearchRequest>();
            var searchResults = await context.CallActivityAsync<SearchResultSet>(
                nameof(SearchActivityFunctions.RunMatchingAlgorithm),
                new IdentifiedSearchRequest {Id = context.InstanceId, SearchRequest = searchRequest}
            );

            return searchResults.SearchResults.Select(r => r.DonorId.ToString()).ToList();
        }
    }
}