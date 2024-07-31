using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ServiceBus;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly string repeatSearchRequestsTopicName;
        private readonly string resultsNotificationTopicName;
        private readonly ITopicClientFactory topicClientFactory;

        public RepeatSearchServiceBusClient(MessagingServiceBusSettings messagingServiceBusSettings, [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory)
        {
            repeatSearchRequestsTopicName = messagingServiceBusSettings.RepeatSearchRequestsTopic;
            resultsNotificationTopicName = messagingServiceBusSettings.RepeatSearchMatchingResultsTopic;
            this.topicClientFactory = topicClientFactory;
        }

        public async Task PublishToRepeatSearchRequestsTopic(IdentifiedRepeatSearchRequest searchRequest)
        {
            var json = JsonConvert.SerializeObject(searchRequest);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            await using var client = topicClientFactory.BuildTopicClient(repeatSearchRequestsTopicName);
            await client.SendAsync(message);
        }

        public async Task PublishToResultsNotificationTopic(MatchingResultsNotification matchingResultsNotification)
        {
            var json = JsonConvert.SerializeObject(matchingResultsNotification);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
            {
                ApplicationProperties =
                {
                    {nameof(MatchingResultsNotification.SearchRequestId), matchingResultsNotification.SearchRequestId},
                    {nameof(MatchingResultsNotification.RepeatSearchRequestId), matchingResultsNotification.RepeatSearchRequestId},
                    {nameof(MatchingResultsNotification.WasSuccessful), matchingResultsNotification.WasSuccessful},
                    {nameof(MatchingResultsNotification.NumberOfResults), matchingResultsNotification.NumberOfResults},
                    {nameof(MatchingResultsNotification.MatchingAlgorithmHlaNomenclatureVersion), matchingResultsNotification.MatchingAlgorithmHlaNomenclatureVersion},
                    {nameof(MatchingResultsNotification.ElapsedTime), matchingResultsNotification.ElapsedTime},
                }
            };

            await using var client = topicClientFactory.BuildTopicClient(resultsNotificationTopicName);
            await client.SendAsync(message);
        }
    }
}
