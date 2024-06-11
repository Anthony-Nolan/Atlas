using System.Collections.Concurrent;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        IMessageReceiver GetMessageReceiver(string connectionString, string topicName, string subscriptionName);
    }

    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private static readonly ConcurrentDictionary<string, IMessageReceiver> MessageReceivers = new();

        public IMessageReceiver GetMessageReceiver(string connectionString, string topicName, string subscriptionName)
        {
            var cacheKey = CacheKey(topicName, subscriptionName);

            if (MessageReceivers.TryGetValue(cacheKey, out var messageReceiver))
            {
                return messageReceiver;
            }

            //messageReceiver = new MessageReceiver(
            //    connectionString,
            //    EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName));

            var builder = new ServiceBusConnectionStringBuilder(connectionString);
            builder.TransportType = TransportType.Amqp;



            messageReceiver = new MessageReceiver(new ServiceBusConnection(builder), EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName));

            MessageReceivers.GetOrAdd(cacheKey, messageReceiver);
            return messageReceiver;
        }

        private static string CacheKey(string topic, string subscription)
        {
            return $"topic-{topic}-subscription-{subscription}";
        }
    }
}