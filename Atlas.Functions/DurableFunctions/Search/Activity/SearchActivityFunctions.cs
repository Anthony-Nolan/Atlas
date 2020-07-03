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
        // Matching Algorithm Services
        private readonly ISearchRunner searchRunner;

        // Donor Import services
        private readonly IDonorReader donorReader;

        // Match Prediction services
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        // Atlas.Functions services
        private readonly IResultsUploader searchResultsBlobUploader;
        private readonly IMatchPredictionInputBuilder matchPredictionInputBuilder;
        private readonly IResultsCombiner resultsCombiner;
        private readonly ISearchCompletionMessageSender searchCompletionMessageSender;

        public SearchActivityFunctions(
            // Matching Algorithm Services
            ISearchRunner searchRunner,
            // Donor Import services
            IDonorReader donorReader,
            // Match Prediction services
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            // Atlas.Functions services
            IResultsUploader searchResultsBlobUploader,
            IMatchPredictionInputBuilder matchPredictionInputBuilder,
            IResultsCombiner resultsCombiner,
            ISearchCompletionMessageSender searchCompletionMessageSender)
        {
            this.searchRunner = searchRunner;
            this.donorReader = donorReader;
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.searchResultsBlobUploader = searchResultsBlobUploader;
            this.matchPredictionInputBuilder = matchPredictionInputBuilder;
            this.resultsCombiner = resultsCombiner;
            this.searchCompletionMessageSender = searchCompletionMessageSender;
        }

        [FunctionName(nameof(RunMatchingAlgorithm))]
        public async Task<MatchingAlgorithmResultSet> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            try
            {
                return await searchRunner.RunSearch(searchRequest);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    searchRequest.Id,
                    $"Failed to run matching algorithm.\n Exception: {e.Message}"
                );
                throw;
            }
        }

        [FunctionName(nameof(FetchDonorInformation))]
        public async Task<IDictionary<int, Donor>> FetchDonorInformation([ActivityTrigger] Tuple<string, IEnumerable<int>> searchAndDonorIds)
        {
            var (searchId, donorIds) = searchAndDonorIds;
            try
            {
                return await donorReader.GetDonors(donorIds);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    searchId,
                    $"Failed to fetch donor data for use in match prediction.\n Exception: {e.Message}"
                );
                throw;
            }
        }

        [FunctionName(nameof(BuildMatchPredictionInputs))]
        public async Task<IEnumerable<MatchProbabilityInput>> BuildMatchPredictionInputs(
            [ActivityTrigger] MatchPredictionInputParameters matchPredictionInputParameters
        )
        {
            try
            {
                return matchPredictionInputBuilder.BuildMatchPredictionInputs(matchPredictionInputParameters);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    matchPredictionInputParameters.MatchingAlgorithmResults.SearchRequestId,
                    $"Failed to build match prediction inputs.\n Exception: {e.Message}"
                );
                throw;
            }
        }

        [FunctionName(nameof(RunMatchPrediction))]
        public async Task<MatchProbabilityResponse> RunMatchPrediction([ActivityTrigger] MatchProbabilityInput matchProbabilityInput)
        {
            try
            {
                return await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(matchProbabilityInput);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    matchProbabilityInput.SearchRequestId,
                    $"Failed to run match prediction algorithm.\n Exception: {e.Message}"
                );
                throw;
            }
        }

        [FunctionName(nameof(PersistSearchResults))]
        public async Task PersistSearchResults(
            [ActivityTrigger] Tuple<MatchingAlgorithmResultSet, IDictionary<int, MatchProbabilityResponse>> algorithmResults)
        {
            try
            {
                var (matchingResults, matchPredictionResults) = algorithmResults;
                var resultSet = resultsCombiner.CombineResults(matchingResults, matchPredictionResults);
                await searchResultsBlobUploader.UploadResults(resultSet);
                await searchCompletionMessageSender.PublishResultsMessage(resultSet);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    algorithmResults.Item1.SearchRequestId,
                    $"Failed to persist search results.\n Exception: {e.Message}"
                );
                throw;
            }
        }
    }
}