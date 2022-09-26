using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Clients
{
    public interface IMatchPredictionBusClient
    {
        Task BatchPublishToMatchPredictionRequestsTopic(IEnumerable<IdentifiedMatchPredictionRequest> requests);
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

        public async Task BatchPublishToMatchPredictionRequestsTopic(IEnumerable<IdentifiedMatchPredictionRequest> requests)
        {
            await using var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(requestsTopicName);

            var localQueue = new Queue<ServiceBusMessage>();
            foreach (var request in requests)
            {
                var message = new ServiceBusMessage(JsonConvert.SerializeObject(request));
                localQueue.Enqueue(message);
            }

            while (localQueue.Count > 0)
            {
                using var messageBatch = await sender.CreateMessageBatchAsync();
                while (localQueue.Count > 0 && messageBatch.TryAddMessage(localQueue.Peek()))
                {
                    localQueue.Dequeue();
                }

                await sender.SendMessagesAsync(messageBatch);
            }
        }
    }
}