using System;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Clients.ServiceBus
{
    public interface ISearchServiceBusClient
    {
        Task PublishToSearchRequestsTopic(IdentifiedSearchRequest searchRequest);
        Task PublishToResultsNotificationTopic(MatchingResultsNotification matchingResultsNotification);
    }

    public class SearchServiceBusClient : ISearchServiceBusClient
    {
        private readonly ITopicClientFactory topicClientFactory;
        private readonly string searchRequestsTopicName;
        private readonly string resultsNotificationTopicName;

        public SearchServiceBusClient([FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory, MessagingServiceBusSettings messagingServiceBusSettings)
        {
            this.topicClientFactory = topicClientFactory;

            searchRequestsTopicName = messagingServiceBusSettings.SearchRequestsTopic;
            resultsNotificationTopicName = messagingServiceBusSettings.SearchResultsTopic;
        }

        public async Task PublishToSearchRequestsTopic(IdentifiedSearchRequest searchRequest)
        {
            var json = JsonConvert.SerializeObject(searchRequest);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
            {
                ApplicationProperties =
                {
                    {nameof(IdentifiedSearchRequest)+nameof(IdentifiedSearchRequest.Id), searchRequest.Id}
                }
            };

            await using var client = topicClientFactory.BuildTopicClient(searchRequestsTopicName);
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
                    {nameof(MatchingResultsNotification.WasSuccessful), matchingResultsNotification.WasSuccessful},
                    {nameof(MatchingResultsNotification.NumberOfResults), matchingResultsNotification.NumberOfResults},
                    {nameof(MatchingResultsNotification.MatchingAlgorithmHlaNomenclatureVersion), matchingResultsNotification.MatchingAlgorithmHlaNomenclatureVersion},
                    {nameof(MatchingResultsNotification.ElapsedTime), matchingResultsNotification.ElapsedTime},
                }
            };

            // That should help in investigation of #962. When matching-results-ready queue get two messages for same search id,
            // the fact that messages have the same message id will prove that it happens because client auto retry functionality
            message.MessageId = Guid.NewGuid().ToString();

            await using var client = topicClientFactory.BuildTopicClient(resultsNotificationTopicName);// new TopicClient(connectionString, resultsNotificationTopicName);
            await client.SendAsync(message);
        }
    }
}