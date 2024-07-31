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

        public IMessageReceiver GetMessageReceiver(string topicName, string subscriptionName, int? prefetchCount = null)
        {
            var options = new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter };

            if (prefetchCount is not null)
                options.PrefetchCount = prefetchCount.Value;

            var receiver = new MessageReceiver(client.CreateReceiver(topicName, subscriptionName, options));

            return receiver;
        }
    }
}