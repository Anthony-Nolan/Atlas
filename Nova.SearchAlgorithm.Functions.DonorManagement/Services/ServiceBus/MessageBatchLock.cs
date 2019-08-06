using Microsoft.Azure.ServiceBus.Core;
using NeoSmart.AsyncLock;
using Nova.SearchAlgorithm.Functions.DonorManagement.Exceptions;
using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus
{
    public class MessageBatchLock<T> : IDisposable
    {
        private const double LockTimeFraction = 0.7;

        private readonly MessageReceiver messageReceiver;
        private readonly IEnumerable<ServiceBusMessage<T>> messageBatch;
        private readonly AsyncLock asyncLock = new AsyncLock();

        private Timer locksRenewalTimer;
        private bool messageBatchHasBeenReleased;

        public MessageBatchLock(MessageReceiver messageReceiver, IEnumerable<ServiceBusMessage<T>> messageBatch)
        {
            this.messageReceiver = messageReceiver;
            this.messageBatch = messageBatch;
            InitializeLocksRenewalTimer();
        }

        /// <summary>
        /// Removes messages from the queue.
        /// </summary>
        public async Task CompleteBatchAsync()
        {
            await ReleaseBatchAsync(CompleteAsync, nameof(CompleteAsync));
        }

        /// <summary>
        /// Returns messages to the queue with an incremented delivery count.
        /// </summary>
        public async Task AbandonBatchAsync()
        {
            await ReleaseBatchAsync(AbandonAsync, nameof(AbandonAsync));
        }

        public void Dispose()
        {
            locksRenewalTimer?.Dispose();
        }

        /// <summary>
        /// Automatically renews locks on messages without incrementing their delivery count.
        /// </summary>
        private void InitializeLocksRenewalTimer()
        {
            if (!messageBatch.Any())
            {
                return;
            }

            var renewInterval = CalculateRenewInterval();
            var indefinitePeriod = TimeSpan.FromMilliseconds(-1);

            locksRenewalTimer = new Timer(async state =>
            {
                using (await asyncLock.LockAsync())
                {
                    try
                    {
                        await RenewLocksAsync();
                        locksRenewalTimer.Change(renewInterval, indefinitePeriod);
                    }
                    catch (Exception ex)
                    {
                        throw new MessageBatchException<T>(nameof(RenewLocksAsync), messageBatch, ex);
                    }
                }
            }, null, renewInterval, indefinitePeriod);
        }

        private TimeSpan CalculateRenewInterval()
        {
            var firstLockedUntilUtc = messageBatch
                .OrderBy(m => m.SequenceNumber)
                .First()
                .LockedUntilUtc;

            return new TimeSpan(
                (long)Math.Round(firstLockedUntilUtc.Subtract(DateTime.UtcNow).Ticks * LockTimeFraction, 0, MidpointRounding.AwayFromZero));
        }

        private async Task RenewLocksAsync()
        {
            await ActOnMessagesViaLockToken(messageReceiver.RenewLockAsync);
        }

        private async Task CompleteAsync()
        {
            await ActOnMessagesViaLockToken(messageReceiver.CompleteAsync);
        }

        private async Task AbandonAsync()
        {
            await ActOnMessagesViaLockToken(lockToken => messageReceiver.AbandonAsync(lockToken));
        }

        private async Task ActOnMessagesViaLockToken(Func<string, Task> messageFunc)
        {
            await Task.WhenAll(messageBatch.Select(m => messageFunc(m.LockToken)));
        }

        private async Task ReleaseBatchAsync(Func<Task> releaseBatchFunc, string funcName)
        {
            using (await asyncLock.LockAsync())
            {
                if (messageBatchHasBeenReleased)
                {
                    return;
                }

                try
                {
                    await releaseBatchFunc();
                }
                catch (Exception ex)
                {
                    throw new MessageBatchException<T>(funcName, messageBatch, ex);
                }

                messageBatchHasBeenReleased = true;
            }
        }
    }
}
