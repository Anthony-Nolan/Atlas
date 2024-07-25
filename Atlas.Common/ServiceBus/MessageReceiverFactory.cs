using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName, int? prefetchCount = null);
    }

    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private static readonly ConcurrentDictionary<string, MessageReceiver> MessageReceivers = new();
        private readonly ServiceBusClient client;

        public MessageReceiverFactory(ServiceBusClient client)
        {
            this.client = client;
        }

        public IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName, int? prefetchCount = null)
        {
            var cacheKey = CacheKey(topicName, subscriptionName, prefetchCount);

            if (MessageReceivers.TryGetValue(cacheKey, out var messageReceiver))
            {
                return messageReceiver;
            }

            messageReceiver = new MessageReceiver(CreateReceiver(topicName, subscriptionName, prefetchCount));
            MessageReceivers.GetOrAdd(cacheKey, messageReceiver);
            return messageReceiver;
        }

        private ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName, int? prefetchCount) 
        {
            if (prefetchCount is null)
                return client.CreateReceiver(topicName, subscriptionName);

            var options = new ServiceBusReceiverOptions { PrefetchCount = prefetchCount.Value };
            return client.CreateReceiver(topicName, subscriptionName, options);
        }

        private static string CacheKey(string topic, string subscription, int? prefetchCount)
        {
            return prefetchCount is null
                ? $"topic-{topic}-subscription-{subscription}"
                : $"topic-{topic}-subscription-{subscription}-withPrefetch-{prefetchCount}";
        }
    }
}