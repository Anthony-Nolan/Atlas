using Azure.Messaging.ServiceBus;

namespace Atlas.Common.ServiceBus
{
    public interface ITopicClientFactory
    {
        ITopicClient BuildTopicClient(string topicName);
    }

    internal class TopicClientFactory : ITopicClientFactory
    {
        private readonly ServiceBusClient client;

        public TopicClientFactory(ServiceBusClient client)
        {
            this.client = client;
        }

        /// <inheritdoc />
        public ITopicClient BuildTopicClient(string topicName)
        {
            return new TopicClient(client.CreateSender(topicName));
        }
    }
}