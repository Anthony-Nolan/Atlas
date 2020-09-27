using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.ServiceBus.Models;
using Atlas.Functions.PublicApi.Test.Manual.Models;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;

namespace Atlas.Functions.PublicApi.Test.Manual.Services.ServiceBus
{
    internal interface IMessagesPeeker<T> : IServiceBusPeeker<T>
    {
    }

    internal class MessagesPeeker<T> : ServiceBusPeeker<T>, IMessagesPeeker<T>
    {
        public MessagesPeeker(IMessageReceiverFactory factory, string topicName, string subscriptionName)
            : base(factory, topicName, subscriptionName)
        {
        }
    }

    internal interface IDeadLettersPeeker<T> : IServiceBusPeeker<T>
    {
    }

    internal class DeadLettersPeeker<T> : ServiceBusPeeker<T>, IDeadLettersPeeker<T>
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public DeadLettersPeeker(IDeadLetterReceiverFactory factory, string topicName, string subscriptionName) 
            : base(factory, topicName, subscriptionName)
        {
        }
    }

    internal interface IServiceBusPeeker<T>
    {
        Task<IEnumerable<ServiceBusMessage<T>>> Peek(PeekRequest peekRequest);
    }

    internal abstract class ServiceBusPeeker<T> : IServiceBusPeeker<T>
    {
        private readonly IMessageReceiver messageReceiver;

        protected ServiceBusPeeker(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName)
        {
            messageReceiver = factory.GetMessageReceiver(topicName, subscriptionName);
        }

        public async Task<IEnumerable<ServiceBusMessage<T>>> Peek(PeekRequest peekRequest)
        {
            var messages = await messageReceiver.PeekBySequenceNumberAsync(peekRequest.FromSequenceNumber, peekRequest.MessageCount);
            return messages?.Select(GetServiceBusMessage) ?? new List<ServiceBusMessage<T>>();
        }

        private static ServiceBusMessage<T> GetServiceBusMessage(Message message)
        {
            var body = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));

            return new ServiceBusMessage<T>
            {
                SequenceNumber = message.SystemProperties.SequenceNumber,
                LockToken = message.SystemProperties.LockToken,
                LockedUntilUtc = message.SystemProperties.LockedUntilUtc,
                DeserializedBody = body
            };
        }
    }
}