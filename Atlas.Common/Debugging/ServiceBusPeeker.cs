using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Debug.Client.Models.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;

namespace Atlas.Common.Debugging
{
    public interface IMessagesPeeker<T> : IServiceBusPeeker<T>
    {
    }

    public class MessagesPeeker<T> : ServiceBusPeeker<T>, IMessagesPeeker<T>
    {
        public MessagesPeeker(IMessageReceiverFactory factory, string topicName, string subscriptionName)
            : base(factory, topicName, subscriptionName)
        {
        }
    }

    public interface IServiceBusPeeker<T>
    {
        Task<PeekServiceBusMessagesResponse<T>> Peek(PeekServiceBusMessagesRequest peekRequest);
    }

    public abstract class ServiceBusPeeker<T> : IServiceBusPeeker<T>
    {
        private readonly IMessageReceiver messageReceiver;

        protected ServiceBusPeeker(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName)
        {
            messageReceiver = factory.GetMessageReceiver(topicName, subscriptionName);
        }

        public async Task<PeekServiceBusMessagesResponse<T>> Peek(PeekServiceBusMessagesRequest peekRequest)
        {
            var messages = new List<T>();
            var fromSequenceNumber = peekRequest.FromSequenceNumber;

            // The message receiver Peek method has an undocumented upper message count limit
            // So, keep fetching until desired total message count reached or no new messages returned
            while (messages.Count < peekRequest.MessageCount)
            {
                var messageCount = peekRequest.MessageCount - messages.Count;
                var batch = await messageReceiver.PeekBySequenceNumberAsync(fromSequenceNumber, messageCount);

                if (!batch.Any())
                {
                    break;
                }

                messages.AddRange(batch.Select(GetServiceBusMessage));
                fromSequenceNumber = batch.Select(m => m.SystemProperties.SequenceNumber).MaxBy(i => i) + 1;
            }

            return new PeekServiceBusMessagesResponse<T>
            {
                MessageCount = messages.Count,
                PeekedMessages = messages
            };
        }

        private static T GetServiceBusMessage(Message message)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
        }
    }
}