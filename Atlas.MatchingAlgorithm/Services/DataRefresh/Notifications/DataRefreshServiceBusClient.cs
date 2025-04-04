using System;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications
{
    internal interface IDataRefreshServiceBusClient
    {
        Task PublishToRequestTopic(ValidatedDataRefreshRequest dataRefreshRequest);
        Task PublishToCompletionTopic(CompletedDataRefresh completedDataRefresh);
    }

    internal sealed class DataRefreshServiceBusClient : IDataRefreshServiceBusClient, IAsyncDisposable
    {
        private readonly ITopicClient requestTopicClient;
        private readonly ITopicClient completionTopicClient;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly ILogger logger;

        public DataRefreshServiceBusClient(
            [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory,
            DataRefreshSettings dataRefreshSettings, ILogger logger)
        {
            requestTopicClient = topicClientFactory.BuildTopicClient(dataRefreshSettings.RequestsTopic);
            completionTopicClient = topicClientFactory.BuildTopicClient(dataRefreshSettings.CompletionTopic);
            sendRetryCount = dataRefreshSettings.SendRetryCount;
            sendRetryCooldownSeconds = dataRefreshSettings.SendRetryCooldownSeconds;
            this.logger = logger;
        }

        public async Task PublishToRequestTopic(ValidatedDataRefreshRequest dataRefreshRequest)
        {
            var message = BuildMessage(dataRefreshRequest);

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await requestTopicClient.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send data refresh request message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
        }

        public async Task PublishToCompletionTopic(CompletedDataRefresh completedDataRefresh)
        {
            var message = BuildMessage(completedDataRefresh);

            using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
            {
                await completionTopicClient.SendWithRetryAndWaitAsync(message, sendRetryCount, sendRetryCooldownSeconds,
                    (exception, retryNumber) => logger.SendTrace($"Could not send data refresh completion message to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}", LogLevel.Warn));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await requestTopicClient.DisposeAsync();
            await completionTopicClient.DisposeAsync();
        }

        private static ServiceBusMessage BuildMessage(object objectToSerialise)
        {
            var json = JsonConvert.SerializeObject(objectToSerialise);
            return new ServiceBusMessage(Encoding.UTF8.GetBytes(json));
        }
    }
}