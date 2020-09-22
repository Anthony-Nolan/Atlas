using System.Text;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications
{
    internal interface IDataRefreshServiceBusClient
    {
        Task PublishToRequestTopic(ValidatedDataRefreshRequest dataRefreshRequest);
        Task PublishToCompletionTopic(CompletedDataRefresh completedDataRefresh);
    }

    internal class DataRefreshServiceBusClient : IDataRefreshServiceBusClient
    {
        private readonly TopicClient requestTopicClient;
        private readonly TopicClient completionTopicClient;

        public DataRefreshServiceBusClient(
            MessagingServiceBusSettings messagingServiceBusSettings, 
            DataRefreshSettings dataRefreshSettings)
        {
            requestTopicClient = new TopicClient(
                messagingServiceBusSettings.ConnectionString, dataRefreshSettings.RequestsTopic);

            completionTopicClient = new TopicClient(
                messagingServiceBusSettings.ConnectionString, dataRefreshSettings.CompletionTopic);
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

        private static Message BuildMessage(object objectToSerialise)
        {
            var json = JsonConvert.SerializeObject(objectToSerialise);
            return new Message(Encoding.UTF8.GetBytes(json));
        }
    }
}