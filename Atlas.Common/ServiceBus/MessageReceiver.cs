using Azure.Messaging.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Common.ServiceBus
{
    public interface IMessageReceiver
    {
        Task<IEnumerable<ServiceBusReceivedMessage>> PeekMessagesAsync(int maxMessages, long fromSequenceNumber);
        Task AbandonMessageAsync(object lockToken);
        Task CompleteMessageAsync(object lockToken);
        Task RenewMessageLockAsync(object lockToken);
        Task<IEnumerable<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int batchSize);
    }

    public sealed class MessageReceiver : IMessageReceiver, IAsyncDisposable
    {
        private readonly ServiceBusReceiver receiver;

        public MessageReceiver(ServiceBusReceiver receiver)
        {
            this.receiver = receiver;
        }

        public async Task<IEnumerable<ServiceBusReceivedMessage>> PeekMessagesAsync(int maxMessages, long fromSequenceNumber) =>
            await receiver.PeekMessagesAsync(maxMessages, fromSequenceNumber);

        public async Task<IEnumerable<ServiceBusReceivedMessage>> ReceiveMessagesAsync(int batchSize) =>
            await receiver.ReceiveMessagesAsync(batchSize);

        public async Task AbandonMessageAsync(object lockToken)
        {
            if (lockToken is not ServiceBusReceivedMessage msg)
                throw new ArgumentException($"Invalid lock token", nameof(lockToken));

            await receiver.AbandonMessageAsync(msg);
        }

        public async Task CompleteMessageAsync(object lockToken)
        {
            if (lockToken is not ServiceBusReceivedMessage msg)
                throw new ArgumentException($"Invalid lock token", nameof(lockToken));

            await receiver.CompleteMessageAsync(msg);
        }

        public async Task RenewMessageLockAsync(object lockToken)
        {
            if (lockToken is not ServiceBusReceivedMessage msg)
                throw new ArgumentException($"Invalid lock token", nameof(lockToken));

            await receiver.RenewMessageLockAsync(msg);
        }

        public ValueTask DisposeAsync() =>
            receiver.DisposeAsync();
    }


}
