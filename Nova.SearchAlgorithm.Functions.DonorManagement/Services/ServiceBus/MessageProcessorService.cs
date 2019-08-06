using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Functions.DonorManagement.Exceptions;
using Nova.SearchAlgorithm.Functions.DonorManagement.Extensions;
using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus
{
    public interface IMessageProcessorService<T>
    {
        Task ProcessMessageBatch(int batchSize, Func<IEnumerable<ServiceBusMessage<T>>, Task> processMessagesFuncAsync);
    }

    public class MessageProcessorService<T> : IMessageProcessorService<T>
    {
        private readonly MessageReceiver messageReceiver;

        public MessageProcessorService(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName)
        {
            messageReceiver = factory.GetMessageReceiver(topicName, subscriptionName);
        }

        public async Task ProcessMessageBatch(int batchSize, Func<IEnumerable<ServiceBusMessage<T>>, Task> processMessagesFuncAsync)
        {
            var batch = await messageReceiver.ReceiveAsync(batchSize);
            var messages = GetServiceBusMessages(batch).ToList();

            try
            {
                await processMessagesFuncAsync(messages);
                await messages.CompleteMessagesAsync(messageReceiver);
            }
            catch (Exception ex)
            {
                await messages.AbandonMessagesAsync(messageReceiver);
                throw new MessageProcessorException<T>(messages, ex);
            }
        }

        private static IEnumerable<ServiceBusMessage<T>> GetServiceBusMessages(IEnumerable<Message> batch)
        {
            var deserializedMessages = new List<ServiceBusMessage<T>>();
            foreach (var message in batch)
            {
                var body = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));
                deserializedMessages.Add(new ServiceBusMessage<T>
                {
                    SequenceNumber = message.SystemProperties.SequenceNumber,
                    LockToken = message.SystemProperties.LockToken,
                    DeserializedBody = body
                });
            }

            return deserializedMessages;
        }
    }
}
