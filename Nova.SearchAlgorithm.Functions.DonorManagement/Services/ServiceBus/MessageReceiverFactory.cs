using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        MessageReceiver GetMessageReceiver(string topicName, string subscriptionName);
    }

    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly string connectionString;

        public MessageReceiverFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public MessageReceiver GetMessageReceiver(string topicName, string subscriptionName)
        {
            return new MessageReceiver(
                connectionString,
                EntityNameHelper.FormatSubscriptionPath(topicName, subscriptionName));
        }
    }
}
