using Microsoft.Azure.ServiceBus;

namespace Atlas.Common.ServiceBus
{
    public interface ITopicClientFactory
    {
        ITopicClient BuildTopicClient(string connectionString, string topicName);
    }

    internal class TopicClientFactory : ITopicClientFactory
    {
        /// <inheritdoc />
        public ITopicClient BuildTopicClient(string connectionString, string topicName) => new TopicClient(connectionString, topicName);
    }
}