using System;
using System.Text;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;

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
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly ILogger logger;

        public SearchServiceBusClient([FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory, MessagingServiceBusSettings messagingServiceBusSettings, ILogger logger)
        {
            this.topicClientFactory = topicClientFactory;

            searchRequestsTopicName = messagingServiceBusSettings.SearchRequestsTopic;
            resultsNotificationTopicName = messagingServiceBusSettings.SearchResultsTopic;
            sendRetryCount = messagingServiceBusSettings.SendRetryCount;
            sendRetryCooldownSeconds = messagingServiceBusSettings.SendRetryCooldownSeconds;
            this.logger = logger;
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

            int attempt = 1;
            var retryPolicy = Policy
                .Handle<ServiceBusException>()
                .WaitAndRetryAsync(sendRetryCount, _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds));

            await using var client = topicClientFactory.BuildTopicClient(searchRequestsTopicName);

            await retryPolicy.ExecuteAsync(async () =>
            {
                logger.SendTrace("SearchRequest " + searchRequest.Id + "Attempt " + attempt, attempt == 1 ? LogLevel.Verbose : LogLevel.Warn);
                attempt++;
                await client.SendAsync(message);
            });
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

            int attempt = 1;
            var retryPolicy = Policy
                .Handle<ServiceBusException>()
                .WaitAndRetryAsync(sendRetryCount, _ => TimeSpan.FromSeconds(sendRetryCooldownSeconds));

            await using var client = topicClientFactory.BuildTopicClient(resultsNotificationTopicName);

            await retryPolicy.ExecuteAsync(async () =>
            {
                logger.SendTrace("ResultsNotification " + matchingResultsNotification.SearchRequestId + "Attempt " + attempt,
                    attempt == 1 ? LogLevel.Verbose : LogLevel.Warn);
                attempt++;
                await client.SendAsync(message);
            });
        }
    }
}