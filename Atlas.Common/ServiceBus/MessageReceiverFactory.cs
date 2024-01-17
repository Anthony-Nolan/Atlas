using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName);
    }

    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly string connectionString;

        private readonly IDictionary<string, IMessageReceiver> messageReceivers = new Dictionary<string, IMessageReceiver>();

        public MessageReceiverFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName)
        {
            var cacheKey = CacheKey(topicName, subscriptionName);
            if (!messageReceivers.TryGetValue(cacheKey, out var messageReceiver))
            {
                messageReceiver = new MessageReceiver(
                    connectionString,
                    EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName));
                messageReceivers.Add(cacheKey, messageReceiver);
            }

            return messageReceiver;
        }

        private static string CacheKey(string topic, string subscription)
        {
            return $"topic-{topic}-subscription-{subscription}";
        }
    }
}