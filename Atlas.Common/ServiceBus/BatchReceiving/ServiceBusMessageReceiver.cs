using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;

namespace Atlas.Common.ServiceBus.BatchReceiving
{
    public interface IServiceBusMessageReceiver<T>
    {
        /// <summary>
        /// Fetches Azure service bus messages in batches and transforms them to a message collection of type T.
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to fetch at once.</param>
        /// <param name="prefetchCount">Number of messages to fetch in advance of processing.</param>
        Task<IEnumerable<ServiceBusMessage<T>>> ReceiveMessageBatchAsync(int batchSize, int prefetchCount);
        
        Task RenewMessageLockAsync(string lockToken);
        Task CompleteMessageAsync(string lockToken);
        Task AbandonMessageAsync(string lockToken);
    }

    public class ServiceBusMessageReceiver<T> : IServiceBusMessageReceiver<T>
    {
        private readonly IMessageReceiver messageReceiver;

        public ServiceBusMessageReceiver(
            IMessageReceiverFactory factory,
            string connectionString,
            string topicName,
            string subscriptionName)
        {
            messageReceiver = factory.GetMessageReceiver(connectionString, topicName, subscriptionName);
        }

        public async Task<IEnumerable<ServiceBusMessage<T>>> ReceiveMessageBatchAsync(int batchSize, int prefetchCount)
        {
            messageReceiver.PrefetchCount = prefetchCount;

            var batch = await messageReceiver.ReceiveAsync(batchSize);
            return batch != null
                ? batch.Select(GetServiceBusMessage)
                : new List<ServiceBusMessage<T>>();
        }

        public async Task RenewMessageLockAsync(string lockToken)
        {
            await messageReceiver.RenewLockAsync(lockToken);
        }

        public async Task CompleteMessageAsync(string lockToken)
        {
            await messageReceiver.CompleteAsync(lockToken);
        }

        public async Task AbandonMessageAsync(string lockToken)
        {
            await messageReceiver.AbandonAsync(lockToken);
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