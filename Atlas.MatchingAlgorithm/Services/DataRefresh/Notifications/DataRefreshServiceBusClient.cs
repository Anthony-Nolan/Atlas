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
        Task PublishToCompletionTopic(CompletedDataRefresh completedDataRefresh);
    }

    internal class DataRefreshServiceBusClient : IDataRefreshServiceBusClient
    {
        private readonly TopicClient completionTopicClient;

        public DataRefreshServiceBusClient(
            MessagingServiceBusSettings messagingServiceBusSettings, 
            DataRefreshSettings dataRefreshSettings)
        {
            completionTopicClient = new TopicClient(
                messagingServiceBusSettings.ConnectionString, dataRefreshSettings.CompletionTopic);
        }

        public async Task PublishToCompletionTopic(CompletedDataRefresh completedDataRefresh)
        {
            var json = JsonConvert.SerializeObject(completedDataRefresh);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            await completionTopicClient.SendAsync(message);
        }
    }
}