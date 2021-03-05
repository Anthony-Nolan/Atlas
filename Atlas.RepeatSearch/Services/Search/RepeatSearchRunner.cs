using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Clients.AzureStorage;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Repositories;
using Atlas.RepeatSearch.Models;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchRunner
    {
        Task<MatchingAlgorithmResultSet> RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest);
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

        public RepeatSearchRunner(
            IRepeatSearchServiceBusClient repeatSearchServiceBusClient,
            ISearchService searchService,
            IRepeatSearchResultsBlobStorageClient repeatResultsBlobStorageClient,
            // ReSharper disable once SuggestBaseTypeForParameter
            IMatchingAlgorithmSearchLogger repeatSearchLogger,
            MatchingAlgorithmSearchLoggingContext repeatSearchLoggingContext,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IRepeatSearchHistoryRepository repeatSearchHistoryRepository,
            IRepeatSearchValidator repeatSearchValidator)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
            this.searchService = searchService;
            this.repeatResultsBlobStorageClient = repeatResultsBlobStorageClient;
            this.repeatSearchLogger = repeatSearchLogger;
            this.repeatSearchLoggingContext = repeatSearchLoggingContext;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
            this.repeatSearchHistoryRepository = repeatSearchHistoryRepository;
            this.repeatSearchValidator = repeatSearchValidator;
        }

        public async Task<MatchingAlgorithmResultSet> RunSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest)
        {
            var searchRequestId = identifiedRepeatSearchRequest.OriginalSearchId;
            var repeatSearchId = identifiedRepeatSearchRequest.RepeatSearchId;
            repeatSearchLoggingContext.SearchRequestId = searchRequestId;
            var searchAlgorithmServiceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var hlaNomenclatureVersion = hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion();
            repeatSearchLoggingContext.HlaNomenclatureVersion = hlaNomenclatureVersion;

            try
            {
                await repeatSearchValidator.ValidateRepeatSearchAndThrow(identifiedRepeatSearchRequest.RepeatSearchRequest);


                await RecordRepeatSearch(identifiedRepeatSearchRequest);

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var results = (await searchService.Search(identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate)).ToList();
                stopwatch.Stop();

                var blobContainerName = repeatResultsBlobStorageClient.GetResultsContainerName();

                var searchResultSet = new MatchingAlgorithmResultSet
                {
                    SearchRequestId = searchRequestId,
                    RepeatSearchId = repeatSearchId,
                    MatchingAlgorithmResults = results,
                    ResultCount = results.Count,
                    HlaNomenclatureVersion = hlaNomenclatureVersion,
                    BlobStorageContainerName = blobContainerName,
                };

                await repeatResultsBlobStorageClient.UploadResults(searchResultSet);

                var notification = new MatchingResultsNotification
                {
                    SearchRequest = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchRequest,
                    RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId,
                    SearchRequestId = searchRequestId,
                    MatchingAlgorithmServiceVersion = searchAlgorithmServiceVersion,
                    HlaNomenclatureVersion = hlaNomenclatureVersion,
                    WasSuccessful = true,
                    NumberOfResults = results.Count,
                    BlobStorageContainerName = blobContainerName,
                    BlobStorageResultsFileName = searchResultSet.ResultsFileName,
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
                    HlaNomenclatureVersion = hlaNomenclatureVersion
                };
                await repeatSearchServiceBusClient.PublishToResultsNotificationTopic(notification);
                throw;
            }
        }

        private async Task RecordRepeatSearch(IdentifiedRepeatSearchRequest identifiedRepeatSearchRequest)
        {
            var historyRecord = new RepeatSearchHistoryRecord
            {
                DateCreated = DateTimeOffset.UtcNow,
                // ReSharper disable once PossibleInvalidOperationException - validation should have caught nulls by now
                SearchCutoffDate = identifiedRepeatSearchRequest.RepeatSearchRequest.SearchCutoffDate.Value,
                OriginalSearchRequestId = identifiedRepeatSearchRequest.OriginalSearchId,
                RepeatSearchRequestId = identifiedRepeatSearchRequest.RepeatSearchId
            };

            await repeatSearchHistoryRepository.RecordRepeatSearchRequest(historyRecord);
        }
    }
}