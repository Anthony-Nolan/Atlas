using System;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
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

        public DataRefreshServiceBusClient(
            [FromKeyedServices(typeof(MessagingServiceBusSettings))]ITopicClientFactory topicClientFactory,
            DataRefreshSettings dataRefreshSettings)
        {
            requestTopicClient = topicClientFactory.BuildTopicClient(
                dataRefreshSettings.RequestsTopic);

            completionTopicClient = topicClientFactory.BuildTopicClient(
                dataRefreshSettings.CompletionTopic);
        }

        public async Task PublishToRequestTopic(ValidatedDataRefreshRequest dataRefreshRequest)
        {
            var message = BuildMessage(dataRefreshRequest);
            await requestTopicClient.SendAsync(message);
        }

        public async Task PublishToCompletionTopic(CompletedDataRefresh completedDataRefresh)
        {
            var message = BuildMessage(completedDataRefresh);
            await completionTopicClient.SendAsync(message);
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