using Atlas.Common.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Atlas.ManualTesting.Services.ServiceBus
{
    internal interface IDeadLetterReceiverFactory : IMessageReceiverFactory
    {
    }

    internal class DeadLetterReceiverFactory : IDeadLetterReceiverFactory
    {
        public IMessageReceiver GetMessageReceiver(string connectionString, string topicName, string subscriptionName)
        {
            return new MessageReceiver(connectionString, DeadLetterPath(topicName, subscriptionName));
        }

        private static string DeadLetterPath(string topic, string subscription)
        {
            return EntityNameHelper.FormatDeadLetterPath(
                EntityNameHelper.FormatSubscriptionPath(topic, subscription));
        }
    }
}