using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Clients.AzureStorage;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Services.ResultSetTracking;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchRunner
    {
        Task<ResultSet<MatchingAlgorithmResult>> RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest);
    }

    public class RepeatSearchRunner : IRepeatSearchRunner
    {
        private readonly IRepeatSearchServiceBusClient repeatSearchServiceBusClient;
        private readonly ISearchService searchService;
        private readonly IRepeatSearchResultsBlobStorageClient repeatResultsBlobStorageClient;
        private readonly ILogger repeatSearchLogger;
        private readonly MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;
        private readonly IRepeatSearchHistoryRepository repeatSearchHistoryRepository;
        private readonly IRepeatSearchValidator repeatSearchValidator;
        private readonly IRepeatSearchDifferentialCalculator repeatSearchDifferentialCalculator;
        private readonly IOriginalSearchResultSetTracker originalSearchResultSetTracker;

        public RepeatSearchRunner(
            IRepeatSearchServiceBusClient repeatSearchServiceBusClient,
            ISearchService searchService,
            IRepeatSearchResultsBlobStorageClient repeatResultsBlobStorageClient,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger repeatSearchLogger,
            MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IRepeatSearchHistoryRepository repeatSearchHistoryRepository,
            IRepeatSearchValidator repeatSearchValidator,
            IRepeatSearchDifferentialCalculator repeatSearchDifferentialCalculator,
            IOriginalSearchResultSetTracker originalSearchResultSetTracker)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
            this.searchService = searchService;
            this.repeatResultsBlobStorageClient = repeatResultsBlobStorageClient;
            this.repeatSearchLogger = repeatSearchLogger;
            this.repeatSearchLoggingContext = repeatSearchLoggingContext;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
            this.repeatSearchHistoryRepository = repeatSearchHistoryRepository;
            this.repeatSearchValidator = repeatSearchValidator;
            this.repeatSearchDifferentialCalculator = repeatSearchDifferentialCalculator;
            this.originalSearchResultSetTracker = originalSearchResultSetTracker;
        }

        public async Task<ResultSet<MatchingAlgorithmResult>> RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest)
        {
            await repeatSearchValidator.ValidateRepeatSearchAndThrow(identifiedRepeatSearchRequest.RepeatSearchRequest);

            var searchRequestId = identifiedRepeatSearchRequest.OriginalSearchId;
            var repeatSearchId = identifiedRepeatSearchRequest.RepeatSearchId;

            repeatSearchLoggingContext.SearchRequestId = searchRequestId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            repeatSearchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            try
            {
                // ReSharper disable once PossibleInvalidOperationException - validation has ensured this is not null.
                var searchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest, searchCutoffDate))
                    .ToList();

                var diff = await CalculateAndStoreResultsDiff(searchRequestId, results, searchCutoffDate);

                await RecordRepeatSearch(identifiedRepeatSearchRequest, diff);

                stopwatch.Stop();

                var blobContainerName = repeatResultsBlobStorageClient.GetResultsContainerName();

                var searchResultSet = new RepeatMatchingAlgorithmResultSet
                {
                    SearchRequestId = searchRequestId,
                    RepeatSearchId = repeatSearchId,
                    Results = results,
                    TotalResults = results.Count,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    BlobStorageContainerName = blobContainerName,
                    NoLongerMatchingDonors = diff.RemovedResults.ToList()
                };

                await repeatResultsBlobStorageClient.UploadResults(searchResultSet);

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                    SearchRequestId = searchRequestId,
                    MatchingAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = blobContainerName,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    ElapsedTime = stopwatch.Elapsed
                };
                await repeatSearchServiceBusClient.PublishToResultsNotificationTopic(notification);
                return searchResultSet;
            }
            catch (Exception e)
            {
                repeatSearchLogger.SendTrace($"Failed to run search with id {searchRequestId}. Exception: {e}", LogLevel.Error);
                var notification = new MatchingResultsNotification
                {
                    WasSuccessful = false,
                    SearchRequestId = searchRequestId,
                    RepeatSearchRequestId = repeatSearchId,
                    MatchingAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    MatchingAlgorithmHlaNomenclatureVersion = hlaNomenclatureVersion
                };
                await repeatSearchServiceBusClient.PublishToResultsNotificationTopic(notification);
                throw;
            }
        }

        private async Task<SearchResultDifferential> CalculateAndStoreResultsDiff(
            string searchRequestId,
            List<MatchingAlgorithmResult> results,
            DateTimeOffset searchCutoffDate)
        {
            using (repeatSearchLogger.RunTimed("Calculate and apply result diff to canonical result set"))
            {
                var diff = await repeatSearchDifferentialCalculator.CalculateDifferential(searchRequestId, results, searchCutoffDate);
                await originalSearchResultSetTracker.ApplySearchResultDiff(searchRequestId, diff);

                repeatSearchLogger.SendTrace(
                    $"Donor Result Diff Calculated. {diff.NewResults.Count} new results. {diff.UpdatedResults.Count} updated results. {diff.RemovedResults.Count} removed results."
                );

                return diff;
            }
        }

        private async Task RecordRepeatSearch(
            IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest,
            SearchResultDifferential searchResultDifferential)
        {
            var historyRecord = new RepeatSearchHistoryRecord
            {
                DateCreated = DateTimeOffset.UtcNow,
                // ReSharper disable once PossibleInvalidOperationException - validation should have caught nulls by now
                SearchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value,
                OriginalSearchRequestId = identifiedRepeatSearchRequest.OriginalSearchId,
                RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                AddedResultCount = searchResultDifferential.NewResults.Count,
                UpdatedResultCount = searchResultDifferential.UpdatedResults.Count,
                RemovedResultCount = searchResultDifferential.RemovedResults.Count,
            };

            await repeatSearchHistoryRepository.RecordRepeatSearchRequest(historyRecord);
        }
    }
}