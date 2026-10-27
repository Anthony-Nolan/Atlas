using Azure.Messaging.ServiceBus;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageReceiverFactory
    {
        /// <summary>
        /// Creates a new receiver. The caller owns it and must dispose it:
        /// an open receiver keeps its link, and the broker can lock messages on that link that nobody will process.
        /// </summary>
        IMessageReceiver CreateMessageReceiver(string topicName, string subscriptionName);
    }

    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly ServiceBusClient client;

        public MessageReceiverFactory(ServiceBusClient client)
        {
            this.client = client;
        }

        public IMessageReceiver CreateMessageReceiver(string topicName, string subscriptionName)
        {
            return new MessageReceiver(client.CreateReceiver(topicName, subscriptionName));
        }
    }
}