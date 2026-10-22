using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName);
    }

    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private static readonly ConcurrentDictionary<string, MessageReceiver> MessageReceivers = new();
        private readonly ServiceBusClient client;

        public MessageReceiverFactory(ServiceBusClient client)
        {
            this.client = client;
        }

        public IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName)
        {
            var cacheKey = CacheKey(topicName, subscriptionName);

            if (MessageReceivers.TryGetValue(cacheKey, out var messageReceiver))
            {
                return messageReceiver;
            }

            messageReceiver = new MessageReceiver(client.CreateReceiver(topicName, subscriptionName));
            MessageReceivers.GetOrAdd(cacheKey, messageReceiver);
            return messageReceiver;
        }

        private static string CacheKey(string topic, string subscription)
        {
            return $"topic-{topic}-subscription-{subscription}";
        }
    }
}