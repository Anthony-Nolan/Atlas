using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Functions.Models.Search.Results;
using Atlas.Functions.Settings;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Atlas.Functions.Services
{
    public interface ISearchCompletionMessageSender
    {
        Task PublishResultsMessage(SearchResultSet searchResultSet, DateTime searchInitiationTime);
        Task PublishFailureMessage(string searchId, string failureMessage);
    }

    internal class SearchCompletionMessageSender : ISearchCompletionMessageSender
    {
        private readonly ILogger logger;
        private readonly string connectionString;
        private readonly string resultsNotificationTopicName;

        public SearchCompletionMessageSender(IOptions<MessagingServiceBusSettings> messagingServiceBusSettings, ILogger logger)
        {
            this.logger = logger;
            connectionString = messagingServiceBusSettings.Value.ConnectionString;
            resultsNotificationTopicName = messagingServiceBusSettings.Value.SearchResultsTopic;
        }

        /// <inheritdoc />
        public async Task PublishResultsMessage(SearchResultSet searchResultSet, DateTime searchInitiationTime)
        {
            var searchTime = DateTime.UtcNow.Subtract(searchInitiationTime);
            logger.SendTrace(
                $"Search Request: {searchResultSet.SearchRequestId} finished. Matched {searchResultSet.TotalResults} donors in {searchTime} total.",
                LogLevel.Info,
                new Dictionary<string, string>
                {
                    {"SearchRequestId", searchResultSet.SearchRequestId},
                    {"Donors", searchResultSet.TotalResults.ToString()},
                    {"Milliseconds", searchTime.Milliseconds.ToString()},
                });

            var searchResultsNotification = new SearchResultsNotification
            {
                WasSuccessful = true,
                HlaNomenclatureVersion = searchResultSet.HlaNomenclatureVersion,
                NumberOfResults = searchResultSet.TotalResults,
                ResultsFileName = searchResultSet.ResultsFileName,
                SearchRequestId = searchResultSet.SearchRequestId,
                BlobStorageContainerName = searchResultSet.BlobStorageContainerName,
                MatchingAlgorithmTime = searchResultSet.MatchingAlgorithmTime,
                MatchPredictionTime = searchResultSet.MatchPredictionTime,
                OverallSearchTime = searchTime
            };

            await SendNotificationMessage(searchResultsNotification);
        }

        /// <inheritdoc />
        public async Task PublishFailureMessage(string searchId, string failureMessage)
        {
            var searchResultsNotification = new SearchResultsNotification
            {
                WasSuccessful = false,
                SearchRequestId = searchId,
                FailureMessage = failureMessage
            };

            await SendNotificationMessage(searchResultsNotification);
        }

        private async Task SendNotificationMessage(SearchResultsNotification searchResultsNotification)
        {
            var json = JsonConvert.SerializeObject(searchResultsNotification);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            var client = new TopicClient(connectionString, resultsNotificationTopicName);
            await client.SendAsync(message);
        }
    }
}