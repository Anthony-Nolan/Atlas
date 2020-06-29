using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Extensions;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
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
        public static async Task<List<object>> SearchOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var searchRequest = context.GetInput<SearchRequest>();
            var searchResults = await context.CallActivityAsync<SearchResultSet>(
                nameof(SearchActivityFunctions.RunMatchingAlgorithm),
                new IdentifiedSearchRequest {Id = context.InstanceId, SearchRequest = searchRequest}
            );

            var matchPredictionResults = (await Task.WhenAll(
                searchResults.SearchResults.Select(r => RunMatchPrediction(context, searchRequest, r))
            )).ToDictionary();

           return searchResults.SearchResults
                .Select(r => new {MatchResult = r, MPAResult = matchPredictionResults[r.DonorId]} as object)
                .ToList();
        }

        /// <returns>A Task returning a Key Value pair of Atlas Donor ID, and match prediction response.</returns>
        private static async Task<KeyValuePair<int, MatchProbabilityResponse>> RunMatchPrediction(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            SearchResult matchingResult)
        {
            var matchPredictionResult = await context.CallActivityAsync<MatchProbabilityResponse>(
                nameof(SearchActivityFunctions.RunMatchPrediction),
                new MatchProbabilityInput
                {
                    // TODO: ATLAS-236: Get donor HLA from result model
                    DonorHla = new PhenotypeInfo<string>
                    {
                        A = new LocusInfo<string>("*01:01:01"),
                        B = new LocusInfo<string>("*08:182"),
                        C = new LocusInfo<string>("*07:02:80"),
                        Dqb1 = new LocusInfo<string>("*06:01:03"),
                        Drb1 = new LocusInfo<string>("*11:129"),
                    },
                    PatientHla = searchRequest.SearchHlaData.ToPhenotypeInfo(),
                    // TODO: ATLAS-236: Get nomenclature version from search results
                    HlaNomenclatureVersion = "3400"
                }
            );
            return new KeyValuePair<int, MatchProbabilityResponse>(matchingResult.DonorId, matchPredictionResult);
        }
    }
}