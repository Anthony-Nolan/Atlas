using System;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ServiceBus;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;

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
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;

        public RepeatSearchServiceBusClient(MessagingServiceBusSettings messagingServiceBusSettings, [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory)
        {
            repeatSearchRequestsTopicName = messagingServiceBusSettings.RepeatSearchRequestsTopic;
            resultsNotificationTopicName = messagingServiceBusSettings.RepeatSearchMatchingResultsTopic;
            sendRetryCount = messagingServiceBusSettings.SendRetryCount;
            sendRetryCooldownSeconds = messagingServiceBusSettings.SendRetryCooldownSeconds;
            this.topicClientFactory = topicClientFactory;
        }

        public async Task PublishToRepeatSearchRequestsTopic(IdentifiedRepeatSearchRequest searchRequest)
        {
            var json = JsonConvert.SerializeObject(searchRequest);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            var retryPolicy = Policy
                .Handle<ServiceBusException>()
                .WaitAndRetryAsync(sendRetryCount, _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds));

            await using var client = topicClientFactory.BuildTopicClient(repeatSearchRequestsTopicName);
            await retryPolicy.ExecuteAsync(async () => await client.SendAsync(message));
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
