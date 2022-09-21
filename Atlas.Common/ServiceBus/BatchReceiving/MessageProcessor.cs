using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus.Exceptions;
using Atlas.Common.ServiceBus.Models;
using Atlas.Common.Utils.Extensions;

namespace Atlas.Common.ServiceBus.BatchReceiving
{
    public interface IMessageProcessor<T>
    {
        Task ProcessAllMessagesInBatches_Async(
            Func<IEnumerable<ServiceBusMessage<T>>, Task> processMessagesFuncAsync,
            int batchSize,
            int prefetchCount = 0);
    }

    public class MessageProcessor<T> : IMessageProcessor<T>
    {
        private readonly IServiceBusMessageReceiver<T> messageReceiver;

        public MessageProcessor(IServiceBusMessageReceiver<T> messageReceiver)
        {
            this.messageReceiver = messageReceiver;
        }

        /// <summary>
        /// Locks a batch of service bus messages, and performs processing based on the passed delegate
        /// </summary>
        /// <param name="processMessagesFuncAsync">Function that will be run on the message batches</param>
        /// <param name="batchSize">Maximum number of messages to fetch at once</param>
        /// <param name="prefetchCount">Number of messages to fetch in advance of processing</param>
        /// <exception cref="MessageBatchException{T}"></exception>
        public async Task ProcessAllMessagesInBatches_Async(
            Func<IEnumerable<ServiceBusMessage<T>>, Task> processMessagesFuncAsync,
            int batchSize,
            int prefetchCount)
        {
            var messages = (await messageReceiver.ReceiveMessageBatchAsync(batchSize, prefetchCount)).ToList();

            if (messages.IsNullOrEmpty())
            {
                return;
            }

            using (var messageBatchLock = new MessageBatchLock<T>(messageReceiver, messages))
            {
                try
                {
                    await processMessagesFuncAsync(messages);
                }
                catch (Exception ex)
                {
                    await messageBatchLock.AbandonBatchAsync();
                    throw new MessageBatchException<T>(nameof(ProcessAllMessagesInBatches_Async), messages, ex);
                }

                await messageBatchLock.CompleteBatchAsync();
            }
        }
    }
}