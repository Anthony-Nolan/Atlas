using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.Functions.Models;
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
        public async Task<TimedResultSet<MatchingAlgorithmResultSet>> RunMatchingAlgorithm([ActivityTrigger] IdentifiedSearchRequest searchRequest)
        {
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = await searchRunner.RunSearch(searchRequest);
                
                return new TimedResultSet<MatchingAlgorithmResultSet>
                {
                    ElapsedTime = stopwatch.Elapsed,
                    FinishedTimeUtc = DateTime.UtcNow,
                    ResultSet = results
                };
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
        public async Task<TimedResultSet<IDictionary<int, Donor>>> FetchDonorInformation([ActivityTrigger] Tuple<string, IEnumerable<int>> searchAndDonorIds)
        {
            var (searchId, donorIds) = searchAndDonorIds;
            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                var donorInfo = await donorReader.GetDonors(donorIds);
                
                return new TimedResultSet<IDictionary<int, Donor>>
                {
                    ElapsedTime = stopwatch.Elapsed,
                    FinishedTimeUtc = DateTime.UtcNow,
                    ResultSet = donorInfo,
                };
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
        public async Task<IEnumerable<SingleDonorMatchProbabilityInput>> BuildMatchPredictionInputs(
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
        public async Task<MatchProbabilityResponse> RunMatchPrediction([ActivityTrigger] SingleDonorMatchProbabilityInput singleDonorMatchProbabilityInput)
        {
            try
            {
                return await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(singleDonorMatchProbabilityInput);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    singleDonorMatchProbabilityInput.SearchRequestId,
                    $"Failed to run match prediction algorithm.\n Exception: {e.Message}"
                );
                throw;
            }
        }

        [FunctionName(nameof(PersistSearchResults))]
        public async Task PersistSearchResults([ActivityTrigger] PersistSearchResultsParameters parameters)
        {
            try
            {
                var resultSet = resultsCombiner.CombineResults(parameters);
                await searchResultsBlobUploader.UploadResults(resultSet);
                await searchCompletionMessageSender.PublishResultsMessage(resultSet);
            }
            catch (Exception e)
            {
                await searchCompletionMessageSender.PublishFailureMessage(
                    parameters.MatchingAlgorithmResultSet.ResultSet.SearchRequestId,
                    $"Failed to persist search results.\n Exception: {e.Message}"
                );
                throw;
            }
        }

        /// <summary>
        /// Parameters wrapped in single object as Azure Activity functions may only have one parameter.
        /// </summary>
        public class PersistSearchResultsParameters
        {
            public TimedResultSet<MatchingAlgorithmResultSet> MatchingAlgorithmResultSet { get; set; }

            /// <summary>
            /// Keyed by ATLAS ID
            /// </summary>
            public TimedResultSet<Dictionary<int, MatchProbabilityResponse>> MatchPredictionResults { get; set; }

            /// <summary>
            /// Keyed by ATLAS ID
            /// </summary>
            public IDictionary<int, Donor> DonorInformation { get; set; }
        }
    }
}