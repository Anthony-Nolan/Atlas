using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Functions.DonorManagement.Exceptions;
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
            var messages = (await GetServiceBusMessages(batchSize)).ToList();

            using (var messageBatchLock = new MessageBatchLock<T>(messageReceiver, messages))
            {
                try
                {
                    await processMessagesFuncAsync(messages);
                }
                catch (Exception ex)
                {
                    await messageBatchLock.AbandonBatchAsync();
                    throw new MessageBatchException<T>(nameof(ProcessMessageBatch), messages, ex);
                }

                await messageBatchLock.CompleteBatchAsync();
            }
        }

        private async Task<IEnumerable<ServiceBusMessage<T>>> GetServiceBusMessages(int batchSize)
        {
            var batch = await messageReceiver.ReceiveAsync(batchSize);
            return batch != null
                ? batch.Select(GetServiceBusMessage)
                : new List<ServiceBusMessage<T>>();
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
