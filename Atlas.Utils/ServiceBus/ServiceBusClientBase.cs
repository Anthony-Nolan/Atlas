using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace Atlas.Utils.ServiceBus
{
    public abstract class ServiceBusClientBase
    {
        private readonly string connectionString;

        protected ServiceBusClientBase(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected async Task PublishToTopic(string topicName, Message message)
        {
            var client = new TopicClient(connectionString, topicName);
            await client.SendAsync(message);
        }

        protected async Task PublishToQueue(string queueName, Message message)
        {
            var client = new QueueClient(connectionString, queueName);
            await client.SendAsync(message);
        }
    }
}