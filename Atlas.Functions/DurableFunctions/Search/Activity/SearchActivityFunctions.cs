using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Functions.Services;
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
        private readonly IResultsUploader searchResultsBlobUploader;

        public SearchActivityFunctions(
            ISearchRunner searchRunner,
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IResultsUploader searchResultsBlobUploader)
        {
            this.searchRunner = searchRunner;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
        }

        [FunctionName(nameof(RunMatchingAlgorithm))]
        public async Task<MatchingAlgorithmResultSet> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            return await searchRunner.RunSearch(searchRequest);
        }

        [FunctionName(nameof(RunMatchPrediction))]
        public async Task<MatchProbabilityResponse> RunMatchPrediction([ActivityTrigger] MatchProbabilityInput matchProbabilityInput)
        {
            return await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(matchProbabilityInput);
        }

        [FunctionName(nameof(PersistSearchResults))]
        public async Task PersistSearchResults(
            [ActivityTrigger] Tuple<MatchingAlgorithmResultSet, IDictionary<int, MatchProbabilityResponse>> algorithmResults)
        {
            await searchResultsBlobUploader.UploadResults(algorithmResults.Item1, algorithmResults.Item2);
        }
    }
}