using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Clients
{
    public interface IRepeatSearchServiceBusClient
    {
        Task PublishToRepeatSearchRequestsTopic(IdentifiedRepeatSearchRequest searchRequest);
        Task PublishToResultsNotificationTopic(MatchingResultsNotification matchingResultsNotification);
    }

    public class RepeatSearchServiceBusClient : IRepeatSearchServiceBusClient
    {
        private readonly string connectionString;
        private readonly string repeatSearchRequestsTopicName;
        private readonly string resultsNotificationTopicName;

        public RepeatSearchServiceBusClient(MessagingServiceBusSettings messagingServiceBusSettings)
        {
            connectionString = messagingServiceBusSettings.ConnectionString;
            repeatSearchRequestsTopicName = messagingServiceBusSettings.RepeatSearchRequestsTopic;
            resultsNotificationTopicName = messagingServiceBusSettings.RepeatSearchResultsTopic;
        }

        public async Task PublishToRepeatSearchRequestsTopic(IdentifiedRepeatSearchRequest searchRequest)
        {
            var json = JsonConvert.SerializeObject(searchRequest);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            var client = new TopicClient(connectionString, repeatSearchRequestsTopicName);
            await client.SendAsync(message);
        }

        public async Task PublishToResultsNotificationTopic(MatchingResultsNotification matchingResultsNotification)
        {
            var json = JsonConvert.SerializeObject(matchingResultsNotification);
            var message = new Message(Encoding.UTF8.GetBytes(json))
            {
                UserProperties =
                {
                    {nameof(MatchingResultsNotification.SearchRequestId), matchingResultsNotification.SearchRequestId},
                    {nameof(MatchingResultsNotification.RepeatSearchRequestId), matchingResultsNotification.RepeatSearchRequestId},
                    {nameof(MatchingResultsNotification.WasSuccessful), matchingResultsNotification.WasSuccessful},
                    {nameof(MatchingResultsNotification.NumberOfResults), matchingResultsNotification.NumberOfResults},
                    {nameof(MatchingResultsNotification.HlaNomenclatureVersion), matchingResultsNotification.HlaNomenclatureVersion},
                    {nameof(MatchingResultsNotification.ElapsedTime), matchingResultsNotification.ElapsedTime},
                }
            };

            var client = new TopicClient(connectionString, resultsNotificationTopicName);
            await client.SendAsync(message);
        }
    }
}
