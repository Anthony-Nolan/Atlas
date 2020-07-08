using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.DurableFunctions.Search.Activity;
using Atlas.Functions.Models.Search.Requests;
using Atlas.Functions.Models.Search.Results;
using Atlas.Functions.Services;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Common.Models;
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

            var searchResults = await RunMatchingAlgorithm(context, searchRequest);
            var donorInformation = await FetchDonorInformation(context, searchResults);
            var matchPredictionResults = await RunMatchPredictionAlgorithm(context, searchRequest, searchResults, donorInformation);
            await PersistSearchResults(context, searchResults, matchPredictionResults, donorInformation);

            // "return" populates the "output" property on the status check GET endpoint set up by the durable functions framework
            return new SearchOrchestrationOutput
            {
                MatchingDonorCount = searchResults.ResultCount,
                MatchingResultFileName = searchResults.ResultsFileName,
                MatchingResultBlobContainer = searchResults.BlobStorageContainerName,
                HlaNomenclatureVersion = searchResults.HlaNomenclatureVersion
            };
        }

        private static async Task<MatchingAlgorithmResultSet> RunMatchingAlgorithm(IDurableOrchestrationContext context, SearchRequest searchRequest)
        {
            return await context.CallActivityAsync<MatchingAlgorithmResultSet>(
                nameof(SearchActivityFunctions.RunMatchingAlgorithm),
                new IdentifiedSearchRequest {Id = context.InstanceId, MatchingRequest = searchRequest.ToMatchingRequest()}
            );
        }

        private static async Task<Dictionary<int, MatchProbabilityResponse>> RunMatchPredictionAlgorithm(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResultSet searchResults,
            Dictionary<int, Donor> donorInformation)
        {
            var matchPredictionInputs = await BuildMatchPredictionInputs(context, searchRequest, searchResults, donorInformation);


            var matchPredictionResults = (await Task.WhenAll(
                matchPredictionInputs.Select(r => RunMatchPredictionForDonor(context, r))
            )).ToDictionary();
            return matchPredictionResults;
        }

        private static async Task<IEnumerable<MatchProbabilityInput>> BuildMatchPredictionInputs(
            IDurableOrchestrationContext context,
            SearchRequest searchRequest,
            MatchingAlgorithmResultSet searchResults,
            Dictionary<int, Donor> donorInformation)
        {
            return await context.CallActivityAsync<IEnumerable<MatchProbabilityInput>>(
                nameof(SearchActivityFunctions.BuildMatchPredictionInputs),
                new MatchPredictionInputParameters
                {
                    SearchRequest = searchRequest,
                    MatchingAlgorithmResults = searchResults,
                    DonorDictionary = donorInformation
                });
        }

        private static async Task<Dictionary<int, Donor>> FetchDonorInformation(
            IDurableOrchestrationContext context,
            MatchingAlgorithmResultSet matchingAlgorithmResults)
        {
            var activityInput = new Tuple<string, IEnumerable<int>>(
                context.InstanceId,
                matchingAlgorithmResults.MatchingAlgorithmResults.Select(r => r.AtlasDonorId)
            );

            return await context.CallActivityAsync<Dictionary<int, Donor>>(
                nameof(SearchActivityFunctions.FetchDonorInformation),
                activityInput
            );
        }

        /// <returns>A Task returning a Key Value pair of Atlas Donor ID, and match prediction response.</returns>
        private static async Task<KeyValuePair<int, MatchProbabilityResponse>> RunMatchPredictionForDonor(
            IDurableOrchestrationContext context,
            MatchProbabilityInput matchProbabilityInput
        )
        {
            var matchPredictionResult = await context.CallActivityAsync<MatchProbabilityResponse>(
                nameof(SearchActivityFunctions.RunMatchPrediction),
                matchProbabilityInput
            );
            return new KeyValuePair<int, MatchProbabilityResponse>(matchProbabilityInput.DonorId, matchPredictionResult);
        }

        private static async Task PersistSearchResults(
            IDurableOrchestrationContext context,
            MatchingAlgorithmResultSet searchResults,
            Dictionary<int, MatchProbabilityResponse> matchPredictionResults,
            Dictionary<int, Donor> donorInformation)
        {
            await context.CallActivityAsync(
                nameof(SearchActivityFunctions.PersistSearchResults),
                new SearchActivityFunctions.PersistSearchResultsParameters
                {
                    DonorInformation = donorInformation,
                    MatchPredictionResults = matchPredictionResults,
                    MatchingAlgorithmResultSet = searchResults
                }
            );
        }
    }
}