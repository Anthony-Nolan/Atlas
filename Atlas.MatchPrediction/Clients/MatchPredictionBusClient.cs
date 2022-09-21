using System.Text;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Clients
{
    public interface IMatchPredictionBusClient
    {
        Task PublishToMatchPredictionRequestsTopic(IdentifiedMatchPredictionRequest request);
    }

    public class MatchPredictionBusClient : IMatchPredictionBusClient
    {
        private readonly string connectionString;
        private readonly string requestsTopicName;

        public MatchPredictionBusClient(
            MessagingServiceBusSettings messagingServiceBusSettings,
            MatchPredictionRequestsSettings matchPredictionRequestsSettings)
        {
            connectionString = messagingServiceBusSettings.ConnectionString;
            requestsTopicName = matchPredictionRequestsSettings.ServiceBusTopic;
        }

        public async Task PublishToMatchPredictionRequestsTopic(IdentifiedMatchPredictionRequest request)
        {
            var json = JsonConvert.SerializeObject(request);
            var message = new Message(Encoding.UTF8.GetBytes(json));

            var client = new TopicClient(connectionString, requestsTopicName);
            await client.SendAsync(message);
        }
    }
}