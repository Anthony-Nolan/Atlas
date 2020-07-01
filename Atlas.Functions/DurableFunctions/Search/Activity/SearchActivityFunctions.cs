using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
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

        private readonly IDonorReader donorReader;

        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        private readonly IResultsUploader searchResultsBlobUploader;
        private readonly IMatchPredictionInputBuilder matchPredictionInputBuilder;

        public SearchActivityFunctions(
            // Matching Algorithm Services
            ISearchRunner searchRunner,
            // Donor Import services
            IDonorReader donorReader,
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            // Atlas.Functions services
            IResultsUploader searchResultsBlobUploader,
            IMatchPredictionInputBuilder matchPredictionInputBuilder)
        {
            this.searchRunner = searchRunner;
            this.donorReader = donorReader;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.matchPredictionInputBuilder = matchPredictionInputBuilder;
        }

        [FunctionName(nameof(RunMatchingAlgorithm))]
        public async Task<MatchingAlgorithmResultSet> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            return await searchRunner.RunSearch(searchRequest);
        }

        [FunctionName(nameof(FetchDonorInformation))]
        public async Task<IDictionary<int, Donor>> FetchDonorInformation([ActivityTrigger] IEnumerable<int> donorIds)
        {
            return await donorReader.GetDonors(donorIds);
        }

        [FunctionName(nameof(FetchDonorInformation))]
        public IEnumerable<MatchProbabilityInput> BuildMatchPredictionInputs(
            [ActivityTrigger] MatchPredictionInputParameters matchPredictionInputParameters
        )
        {
            return matchPredictionInputBuilder.BuildMatchPredictionInputs(matchPredictionInputParameters);
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
            var (matchingResults, matchPredictionResults) = algorithmResults;
            await searchResultsBlobUploader.UploadResults(matchingResults, matchPredictionResults);
        }
    }
}