using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils;
using Atlas.Functions.Models;
using Atlas.Functions.Settings;
using AutoMapper;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services
{
    public interface ISearchCompletionMessageSender
    {
        Task PublishResultsMessage<T>(T searchResultSet, DateTime searchInitiationTime, string resultsBatchFolder) where T : SearchResultSet;
        Task PublishFailureMessage(FailureNotificationRequestInfo requestInfo);
    }

    internal class SearchCompletionMessageSender : ISearchCompletionMessageSender
    {
        private readonly ILogger logger;
        private readonly IMapper mapper;
        private readonly string resultsNotificationTopicName;
        private readonly string repeatResultsNotificationTopicName;
        private readonly ITopicClientFactory topicClientFactory;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;


        public SearchCompletionMessageSender(
            IOptions<MessagingServiceBusSettings> messagingServiceBusSettings,
            ISearchLogger<SearchLoggingContext> logger,
            IMapper mapper,
            [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.topicClientFactory = topicClientFactory;

            resultsNotificationTopicName = messagingServiceBusSettings.Value.SearchResultsTopic;
            repeatResultsNotificationTopicName = messagingServiceBusSettings.Value.RepeatSearchResultsTopic;
            sendRetryCount = messagingServiceBusSettings.Value.SendRetryCount;
            sendRetryCooldownSeconds = messagingServiceBusSettings.Value.SendRetryCooldownSeconds;
        }

        public async Task PublishResultsMessage<T>(T searchResultSet, DateTime searchInitiationTime, string resultsBatchFolder) where T : SearchResultSet
        {
            using (logger.RunTimed($"Publishing results message: {searchResultSet.SearchRequestId}"))
            {
                var orchestrationSearchTime = DateTime.UtcNow.Subtract(searchInitiationTime);
                // Orchestration time covers everything in the orchestration function layer:
                // [matching results download, donor info fetching, match prediction, results upload]
                // It does not cover matching, which happens in another functions app - so we add it on here. 
                // This means we don't track the queue time, on either the matching or orchestration queue - so user observed search time may be
                // slightly longer than this reported time. This should only be noticeably different under high load. 
                var searchTime = searchResultSet.MatchingAlgorithmTime + orchestrationSearchTime;
                var repeatSearchId = searchResultSet is RepeatSearchResultSet repeatSet ? repeatSet.RepeatSearchId : null;
                
                logger.SendTrace(
                    $"Search Request: {searchResultSet.SearchRequestId} finished. Matched {searchResultSet.TotalResults} donors in {searchTime} total.",
                    LogLevel.Info,
                    new Dictionary<string, string>
                    {
                        {nameof(searchResultSet.SearchRequestId), searchResultSet.SearchRequestId},
                        {nameof(RepeatSearchResultSet.RepeatSearchId), repeatSearchId},
                        {"Donors", searchResultSet.TotalResults.ToString()},
                        {nameof(searchTime.Milliseconds), searchTime.Milliseconds.ToString()},
                    });
                
                var searchResultsNotification = new SearchResultsNotification
                {
                    WasSuccessful = true,
                    MatchingAlgorithmHlaNomenclatureVersion = searchResultSet.MatchingAlgorithmHlaNomenclatureVersion,
                    NumberOfResults = searchResultSet.TotalResults,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    SearchRequestId = searchResultSet.SearchRequestId,
                    RepeatSearchRequestId = repeatSearchId,
                    BlobStorageContainerName = searchResultSet.BlobStorageContainerName,
                    MatchingAlgorithmTime = searchResultSet.MatchingAlgorithmTime,
                    MatchPredictionTime = searchResultSet.MatchPredictionTime,
                    OverallSearchTime = searchTime,
                    ResultsBatched = searchResultSet.BatchedResult,
                    BatchFolderName = searchResultSet.BatchedResult && searchResultSet.TotalResults > 0 ? resultsBatchFolder : null
                };
                await SendNotificationMessage(searchResultsNotification);
            }
        }

        /// <inheritdoc />
        public async Task PublishFailureMessage(FailureNotificationRequestInfo requestInfo)
        {
            var searchResultsNotification = new SearchResultsNotification
            {
                WasSuccessful = false,
                SearchRequestId = requestInfo.SearchRequestId,
                RepeatSearchRequestId = requestInfo.RepeatSearchRequestId,
                FailureInfo = mapper.Map<SearchFailureInfo>(requestInfo)
            };

            await SendNotificationMessage(searchResultsNotification);
        }

        private async Task SendNotificationMessage(SearchResultsNotification searchResultsNotification)
        {
            var json = JsonConvert.SerializeObject(searchResultsNotification);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
            {
                ApplicationProperties =
                {
                    {nameof(SearchResultsNotification.SearchRequestId), searchResultsNotification.SearchRequestId},
                    {nameof(SearchResultsNotification.RepeatSearchRequestId), searchResultsNotification.RepeatSearchRequestId},
                    {nameof(SearchResultsNotification.WasSuccessful), searchResultsNotification.WasSuccessful},
                    {nameof(SearchResultsNotification.FailureInfo.WillRetry), searchResultsNotification.FailureInfo?.WillRetry ?? false},
                    {nameof(SearchResultsNotification.NumberOfResults), searchResultsNotification.NumberOfResults},
                    {nameof(SearchResultsNotification.MatchingAlgorithmHlaNomenclatureVersion), searchResultsNotification.MatchingAlgorithmHlaNomenclatureVersion},
                    {nameof(SearchResultsNotification.OverallSearchTime), searchResultsNotification.OverallSearchTime},
                }
            };

            var notificationTopicName = searchResultsNotification.RepeatSearchRequestId != null
                ? repeatResultsNotificationTopicName
                : resultsNotificationTopicName;

            await using var client = topicClientFactory.BuildTopicClient(notificationTopicName);

            await client.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                (exception, retryNumber) => logger.SendTrace($"Could not send search results message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
        }
    }
}