using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ApplicationInsights.Timing;
using Atlas.Functions.Settings;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services
{
    public interface ISearchCompletionMessageSender
    {
        Task PublishResultsMessage(SearchResultSet searchResultSet, DateTime searchInitiationTime);
        Task PublishFailureMessage(string searchId, string repeatSearchId, string failureMessage);
    }

    internal class SearchCompletionMessageSender : ISearchCompletionMessageSender
    {
        private readonly ILogger logger;
        private readonly string connectionString;
        private readonly string resultsNotificationTopicName;
        private readonly string repeatResultsNotificationTopicName;

        public SearchCompletionMessageSender(IOptions<MessagingServiceBusSettings> messagingServiceBusSettings, ILogger logger)
        {
            this.logger = logger;
            connectionString = messagingServiceBusSettings.Value.ConnectionString;
            resultsNotificationTopicName = messagingServiceBusSettings.Value.SearchResultsTopic;
            repeatResultsNotificationTopicName = messagingServiceBusSettings.Value.RepeatSearchResultsTopic;
        }

        /// <inheritdoc />
        public async Task PublishResultsMessage(SearchResultSet searchResultSet, DateTime searchInitiationTime)
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
                logger.SendTrace(
                    $"Search Request: {searchResultSet.SearchRequestId} finished. Matched {searchResultSet.TotalResults} donors in {searchTime} total.",
                    LogLevel.Info,
                    new Dictionary<string, string>
                    {
                        {nameof(searchResultSet.SearchRequestId), searchResultSet.SearchRequestId},
                        {nameof(searchResultSet.RepeatSearchId), searchResultSet.RepeatSearchId},
                        {"Donors", searchResultSet.TotalResults.ToString()},
                        {nameof(searchTime.Milliseconds), searchTime.Milliseconds.ToString()},
                    });
                var searchResultsNotification = new SearchResultsNotification
                {
                    WasSuccessful = true,
                    HlaNomenclatureVersion = searchResultSet.HlaNomenclatureVersion,
                    NumberOfResults = searchResultSet.TotalResults,
                    ResultsFileName = searchResultSet.ResultsFileName,
                    SearchRequestId = searchResultSet.SearchRequestId,
                    RepeatSearchId = searchResultSet.RepeatSearchId,
                    BlobStorageContainerName = searchResultSet.BlobStorageContainerName,
                    MatchingAlgorithmTime = searchResultSet.MatchingAlgorithmTime,
                    MatchPredictionTime = searchResultSet.MatchPredictionTime,
                    OverallSearchTime = searchTime
                };
                await SendNotificationMessage(searchResultsNotification);
            }
        }

        /// <inheritdoc />
        public async Task PublishFailureMessage(string searchId, string repeatSearchId, string failureMessage)
        {
            var searchResultsNotification = new SearchResultsNotification
            {
                WasSuccessful = false,
                SearchRequestId = searchId,
                RepeatSearchId = repeatSearchId,
                FailureMessage = failureMessage
            };

            await SendNotificationMessage(searchResultsNotification);
        }

        private async Task SendNotificationMessage(SearchResultsNotification searchResultsNotification)
        {
            var json = JsonConvert.SerializeObject(searchResultsNotification);
            var message = new Message(Encoding.UTF8.GetBytes(json))
            {
                UserProperties =
                {
                    {nameof(SearchResultsNotification.SearchRequestId), searchResultsNotification.SearchRequestId},
                    {nameof(SearchResultsNotification.RepeatSearchId), searchResultsNotification.RepeatSearchId},
                    {nameof(SearchResultsNotification.WasSuccessful), searchResultsNotification.WasSuccessful},
                    {nameof(SearchResultsNotification.NumberOfResults), searchResultsNotification.NumberOfResults},
                    {nameof(SearchResultsNotification.HlaNomenclatureVersion), searchResultsNotification.HlaNomenclatureVersion},
                    {nameof(SearchResultsNotification.OverallSearchTime), searchResultsNotification.OverallSearchTime},
                }
            };

            var notificationTopicName = searchResultsNotification.RepeatSearchId != null
                ? repeatResultsNotificationTopicName
                : resultsNotificationTopicName;
            
            var client = new TopicClient(connectionString, notificationTopicName);
            await client.SendAsync(message);
        }
    }
}