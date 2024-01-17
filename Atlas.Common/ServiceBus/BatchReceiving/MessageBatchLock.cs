using NeoSmart.AsyncLock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;
using Atlas.Common.ServiceBus.Exceptions;

namespace Atlas.Common.ServiceBus.BatchReceiving
{
    /// <summary>
    /// Maintains a lock on a collection of messages retrieved from an Azure ServiceBus.
    /// Will renew the lock before it expires, for the duration of that lock.
    /// </summary>
    public class MessageBatchLock<T> : IDisposable
    {
        private readonly IServiceBusMessageReceiver<T> messageReceiver;
        private readonly IEnumerable<ServiceBusMessage<T>> messageBatch;
        private readonly double lockTimeFraction;
        private readonly AsyncLock asyncLock = new AsyncLock();

        private Timer locksRenewalTimer;
        private bool messageBatchHasBeenReleased;

        /// <param name="messageBatch">The message batch that has been retrieved from </param>
        /// <param name="lockTimeFraction">Fraction of message lock time to use in lock renewal rate calculation.</param>
        /// <param name="messageReceiver">The object responsible for communicating with Azure regarding the messages in this batch.</param>
        public MessageBatchLock(
            IServiceBusMessageReceiver<T> messageReceiver,
            IEnumerable<ServiceBusMessage<T>> messageBatch,
            double lockTimeFraction = 0.7)
        {
            this.messageReceiver = messageReceiver;
            this.messageBatch = messageBatch;
            this.lockTimeFraction = lockTimeFraction;

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
                    if (messageBatchHasBeenReleased)
                    {
                        return;
                    }

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

            var ticksUntilLockExpiry = firstLockedUntilUtc.Subtract(DateTime.UtcNow).Ticks;

            var interval = (long) Math.Round(ticksUntilLockExpiry * lockTimeFraction, 0, MidpointRounding.AwayFromZero);
            if (interval < 0)
            {
                throw new Exception("Cannot renew locks on a message batch with a lock expiry in the past.");
            }
            
            return new TimeSpan(interval);
        }

        private async Task RenewLocksAsync()
        {
            await ActOnMessagesViaLockToken(messageReceiver.RenewMessageLockAsync);
        }

        private async Task CompleteAsync()
        {
            await ActOnMessagesViaLockToken(messageReceiver.CompleteMessageAsync);
        }

        private async Task AbandonAsync()
        {
            await ActOnMessagesViaLockToken(messageReceiver.AbandonMessageAsync);
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