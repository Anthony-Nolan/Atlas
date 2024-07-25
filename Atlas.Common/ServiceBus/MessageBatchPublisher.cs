using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageBatchPublisher<in T>
    {
        Task BatchPublish(IEnumerable<T> contentToPublish);
    }

    public class MessageBatchPublisher<T> : IMessageBatchPublisher<T>
    {
        private readonly ServiceBusClient client;
        private readonly string topicName;

        public MessageBatchPublisher(ServiceBusClient client, string topicName)
        {
            this.client = client;   
            this.topicName = topicName;
        }

        public async Task BatchPublish(IEnumerable<T> contentToPublish)
        {
            await using var sender = client.CreateSender(topicName);

            var localQueue = new Queue<ServiceBusMessage>();
            foreach (var content in contentToPublish)
            {
                var message = new ServiceBusMessage(JsonConvert.SerializeObject(content));
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