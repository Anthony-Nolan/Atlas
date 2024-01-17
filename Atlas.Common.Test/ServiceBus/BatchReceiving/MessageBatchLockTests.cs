using FluentAssertions;
using Atlas.Common.ServiceBus.BatchReceiving;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;

namespace Atlas.Common.Test.ServiceBus.BatchReceiving
{
    [TestFixture]
    public class MessageBatchLockTests
    {
        private const string LockToken = "lock-token";
        private const string MessageBody = "message-body";
        private const double LockTimeFraction = 1;
        private const int LockRenewalTime = 100;

        private IServiceBusMessageReceiver<string> messageReceiver;
        private IEnumerable<ServiceBusMessage<string>> messages;
        private MessageBatchLock<string> messageBatchLock;

        [SetUp]
        public void SetUp()
        {
            messageReceiver = Substitute.For<IServiceBusMessageReceiver<string>>();
            messages = new List<ServiceBusMessage<string>>
            {
                new ServiceBusMessage<string>
                {
                    LockToken = LockToken,
                    LockedUntilUtc = DateTime.UtcNow.AddMilliseconds(LockRenewalTime),
                    DeserializedBody = MessageBody
                }
            };

            messageBatchLock = new MessageBatchLock<string>(messageReceiver, messages, LockTimeFraction);
        }

        [Test]
        public async Task CompleteBatchAsync_CompletesMessageBatch()
        {
            await messageBatchLock.CompleteBatchAsync();

            await messageReceiver.Received(1).CompleteMessageAsync(Arg.Is<string>(x => x == LockToken));
        }

        [Test]
        public async Task CompleteBatchAsync_MessageBatchAlreadyReleased_DoesNotCompleteMessageBatch()
        {
            await messageBatchLock.AbandonBatchAsync();

            await messageBatchLock.CompleteBatchAsync();

            await messageReceiver.Received(0).CompleteMessageAsync(Arg.Any<string>());
        }

        [Test]
        public async Task AbandonBatchAsync_AbandonsMessageBatch()
        {
            await messageBatchLock.AbandonBatchAsync();

            await messageReceiver.Received(1).AbandonMessageAsync(Arg.Is<string>(x => x == LockToken));
        }

        [Test]
        public async Task AbandonBatchAsync_MessageBatchAlreadyReleased_DoesNotAbandonMessageBatch()
        {
            await messageBatchLock.CompleteBatchAsync();

            await messageBatchLock.AbandonBatchAsync();

            await messageReceiver.Received(0).AbandonMessageAsync(Arg.Any<string>());
        }

        [Test]
        public async Task LocksOnMessageBatchRepeatedlyRenewed()
        {
            const int lockRenewalCount = 2;

            // the number of calls received will depend on lock fraction value used to calculate renewal frequency
            var numberOfCalls = 0;
            await messageReceiver.RenewMessageLockAsync(Arg.Do<string>(x =>
            {
                if (x == LockToken)
                {
                    numberOfCalls++;
                }
            }));
            
            // sleep thread for long enough to allow lock renewal calls to be made
            Thread.Sleep(LockRenewalTime * (lockRenewalCount + 1));

            numberOfCalls.Should().BeGreaterOrEqualTo(lockRenewalCount);
        }

        [Test]
        public async Task MessageBatchCompleted_LocksOnMessageBatchNotRenewed()
        {
            await messageBatchLock.CompleteBatchAsync();

            Thread.Sleep(LockRenewalTime * 2);

            await messageReceiver.Received(0).RenewMessageLockAsync(Arg.Any<string>());
        }

        [Test]
        public async Task MessageBatchAbandoned_LocksOnMessageBatchNotRenewed()
        {
            await messageBatchLock.AbandonBatchAsync();

            Thread.Sleep(LockRenewalTime * 2);

            await messageReceiver.Received(0).RenewMessageLockAsync(Arg.Any<string>());
        }

        [Test]
        public void MessageBatchLockExpired_ThrowSException()
        {
            messages = new List<ServiceBusMessage<string>>
            {
                new ServiceBusMessage<string>
                {
                    LockToken = LockToken,
                    LockedUntilUtc = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                    DeserializedBody = MessageBody
                }
            };
            
            // ReSharper disable once ObjectCreationAsStatement - expected exception will be thrown on construction 
            Assert.Throws<Exception>(() => new MessageBatchLock<string>(messageReceiver, messages, LockTimeFraction));
        }
    }
}
