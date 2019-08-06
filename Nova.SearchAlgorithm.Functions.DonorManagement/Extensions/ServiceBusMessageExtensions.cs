using Microsoft.Azure.ServiceBus.Core;
using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Extensions
{
    public static class ServiceBusMessageExtensions
    {
        /// <summary>
        /// Abandoning messages returns them to the queue with an incremented delivery count.
        /// </summary>
        public static async Task AbandonMessagesAsync<T>(this IEnumerable<ServiceBusMessage<T>> messages, IMessageReceiver messageReceiver)
        {
            await messages.ActOnMessagesViaLockToken(lockToken => messageReceiver.AbandonAsync(lockToken));
        }

        /// <summary>
        /// Completing messages removes them from the queue.
        /// </summary>
        public static async Task CompleteMessagesAsync<T>(this IEnumerable<ServiceBusMessage<T>> messages, IMessageReceiver messageReceiver)
        {
            await messages.ActOnMessagesViaLockToken(messageReceiver.CompleteAsync);
        }

        private static async Task ActOnMessagesViaLockToken<T>(this IEnumerable<ServiceBusMessage<T>> messages, Func<string, Task> messageFunc)
        {
            await Task.WhenAll(messages.Select(m => messageFunc(m.LockToken)));
        }
    }
}
