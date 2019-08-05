using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus
{
    public interface IMessageReceiverService<T>
    {
        Task<IEnumerable<ServiceBusMessage<T>>> ReceiveMessageBatch(int batchSize);
    }

    public class MessageReceiverService<T> : IMessageReceiverService<T>
    {
        private readonly MessageReceiver messageReceiver;

        public MessageReceiverService(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName)
        {
            messageReceiver = factory.GetMessageReceiver(topicName, subscriptionName);
        }

        public async Task<IEnumerable<ServiceBusMessage<T>>> ReceiveMessageBatch(int batchSize)
        {
            var messageBatch = await messageReceiver.ReceiveAsync(batchSize);

            var deserializedMessages = new List<ServiceBusMessage<T>>();
            foreach (var message in messageBatch)
            {
                var body = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                deserializedMessages.Add(new ServiceBusMessage<T>
                {
                    SequenceNumber = message.SystemProperties.SequenceNumber,
                    DeserializedBody = body
                });

                await messageReceiver.CompleteAsync(message.SystemProperties.LockToken);
            }

            return deserializedMessages;
        }
    }
}
