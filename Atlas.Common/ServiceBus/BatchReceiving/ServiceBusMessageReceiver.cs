using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;
using Azure.Messaging.ServiceBus;
using System;

namespace Atlas.Common.ServiceBus.BatchReceiving
{
    /// <remarks>
    /// Owns its receiver: disposing it closes the receiver.
    /// </remarks>
    public interface IServiceBusMessageReceiver<T> : IAsyncDisposable
    {
        /// <summary>
        /// Fetches Azure service bus messages in batches and transforms them to a message collection of type T.
        /// </summary>
        /// <param name="batchSize">Maximum number of messages to fetch at once.</param>
        Task<IEnumerable<DeserializedMessage<T>>> ReceiveMessageBatchAsync(int batchSize);
        
        Task RenewMessageLockAsync(object lockToken);
        Task CompleteMessageAsync(object lockToken);
        Task AbandonMessageAsync(object lockToken);
    }

    public class ServiceBusMessageReceiver<T> : IServiceBusMessageReceiver<T>
    {
        private readonly IMessageReceiver messageReceiver;

        public ServiceBusMessageReceiver(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName
            )
        {
            messageReceiver = factory.CreateMessageReceiver(topicName, subscriptionName);
        }

        public async Task<IEnumerable<DeserializedMessage<T>>> ReceiveMessageBatchAsync(int batchSize)
        {
            var batch = await messageReceiver.ReceiveMessagesAsync(batchSize);
            return batch != null
                ? batch.Select(GetServiceBusMessage)
                : new List<DeserializedMessage<T>>();
        }

        public async Task RenewMessageLockAsync(object lockToken)
        {
            await messageReceiver.RenewMessageLockAsync(lockToken);
        }

        public async Task CompleteMessageAsync(object lockToken)
        {
            await messageReceiver.CompleteMessageAsync(lockToken);
        }

        public async Task AbandonMessageAsync(object lockToken)
        {
            await messageReceiver.AbandonMessageAsync(lockToken);
        }

        public ValueTask DisposeAsync() =>
            messageReceiver.DisposeAsync();

        private static DeserializedMessage<T> GetServiceBusMessage(ServiceBusReceivedMessage message)
        {
            var body = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(message.Body));

            return new DeserializedMessage<T>
            {
                SequenceNumber = message.SequenceNumber,
                LockToken = message, 
                LockedUntilUtc = message.LockedUntil.DateTime,
                DeserializedBody = body
            };
        }
    }
}