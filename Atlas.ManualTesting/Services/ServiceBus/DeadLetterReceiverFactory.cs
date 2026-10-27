using Atlas.Common.ServiceBus;
using Azure.Messaging.ServiceBus;

namespace Atlas.ManualTesting.Services.ServiceBus
{
    internal interface IDeadLetterReceiverFactory : IMessageReceiverFactory
    {
    }

    internal class DeadLetterReceiverFactory : IDeadLetterReceiverFactory
    {
        private readonly ServiceBusClient client;

        public DeadLetterReceiverFactory(ServiceBusClient client)
        {
            this.client = client;
        }

        public IMessageReceiver CreateMessageReceiver(string topicName, string subscriptionName)
        {
            var options = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };

            var receiver = new MessageReceiver(client.CreateReceiver(topicName, subscriptionName, options));

            return receiver;
        }
    }
}