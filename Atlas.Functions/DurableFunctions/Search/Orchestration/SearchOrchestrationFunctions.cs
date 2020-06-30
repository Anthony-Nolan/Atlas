using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Models;
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
    /// Note that orchestration triggered functions will be run multiple times per-request scope, so need to follow the code constraints
    /// as documented by Microsoft https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints.
    /// Logging should be avoided in this function due to this.
    /// Any expensive or non-deterministic code should be called from an Activity function.
    /// </summary>
    public static class SearchOrchestrationFunctions
    {
        [FunctionName(nameof(SearchOrchestrator))]
        public static async Task<SearchOrchestrationOutput> SearchOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context
        )
        {
            var searchRequest = context.GetInput<SearchRequest>();
            var searchResults = await context.CallActivityAsync<MatchingAlgorithmResultSet>(
                nameof(SearchActivityFunctions.RunMatchingAlgorithm),
                new IdentifiedSearchRequest {Id = context.InstanceId, SearchRequest = searchRequest}
            );

            var matchPredictionResults = (await Task.WhenAll(
                searchResults.SearchResults.Select(r => RunMatchPrediction(context, searchRequest, r, searchResults.HlaNomenclatureVersion))
            )).ToDictionary();

            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.PersistSearchResults),
                new Tuple<MatchingAlgorithmResultSet, IDictionary<int, MatchProbabilityResponse>>(searchResults, matchPredictionResults)
            );
            
            // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
            return new SearchOrchestrationOutput
            {
                NumberOfDonors = searchResults.TotalResults,
                MatchingResultFileName = searchResults.ResultsFileName,
                MatchingResultBlobContainer = searchResults.BlobStorageContainerName,
                HlaNomenclatureVersion = searchResults.HlaNomenclatureVersion
            };
        }

        /// <returns>A Task returning a Key Value pair of Atlas Donor ID, and match prediction response.</returns>
        private static async Task<KeyValuePair<int, MatchProbabilityResponse>> RunMatchPrediction(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResult matchingResult,
            string hlaNomenclatureVersion)
        {
            var matchPredictionResult = await context.CallActivityAsync<MatchProbabilityResponse>(
                nameof(SearchActivityFunctions.RunMatchPrediction),
                new MatchProbabilityInput
                {
                    DonorHla = matchingResult.DonorHla,
                    PatientHla = searchRequest.SearchHlaData.ToPhenotypeInfo(),
                    HlaNomenclatureVersion = hlaNomenclatureVersion
                }
            );
            return new KeyValuePair<int, MatchProbabilityResponse>(matchingResult.DonorId, matchPredictionResult);
        }
    }
}