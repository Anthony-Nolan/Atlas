using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils;
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
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly ILogger logger;

        public RepeatSearchServiceBusClient(MessagingServiceBusSettings messagingServiceBusSettings, [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory, ILogger logger)
        {
            repeatSearchRequestsTopicName = messagingServiceBusSettings.RepeatSearchRequestsTopic;
            resultsNotificationTopicName = messagingServiceBusSettings.RepeatSearchMatchingResultsTopic;
            sendRetryCount = messagingServiceBusSettings.SendRetryCount;
            sendRetryCooldownSeconds = messagingServiceBusSettings.SendRetryCooldownSeconds;
            this.topicClientFactory = topicClientFactory;
            this.logger = logger;
        }

        public async Task PublishToRepeatSearchRequestsTopic(IdentifiedRepeatSearchRequest searchRequest)
        {
            var json = JsonConvert.SerializeObject(searchRequest);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json));

            message.ApplicationProperties.Add("SearchRequestId", searchRequest.OriginalSearchId);
            message.ApplicationProperties.Add("RepeatSearchRequestId", searchRequest.RepeatSearchId);

            await using var client = topicClientFactory.BuildTopicClient(repeatSearchRequestsTopicName);

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await client.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send repeat search request message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
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

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await client.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send repeat search matching result message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
        }
    }
}