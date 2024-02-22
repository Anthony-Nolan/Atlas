using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using FluentAssertions;
using Microsoft.Azure.ServiceBus.Core;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Atlas.Common.Test.ServiceBus.BatchReceiving
{
    [TestFixture]
    public class ServiceBusMessageReceiverTests
    {
        private const string ConnectionString = "connectionString";
        private const string TopicName = "topic";
        private const string SubscriptionName = "subscription";

        private IMessageReceiverFactory messageReceiverFactory;
        private IMessageReceiver messageReceiver;
        private IServiceBusMessageReceiver<string> serviceBusMessageReceiver;

        [SetUp]
        public void SetUp()
        {
            messageReceiverFactory = Substitute.For<IMessageReceiverFactory>();
            messageReceiver = Substitute.For<IMessageReceiver>();

            messageReceiverFactory.GetMessageReceiver(ConnectionString, TopicName, SubscriptionName).Returns(messageReceiver);

            serviceBusMessageReceiver = new ServiceBusMessageReceiver<string>(messageReceiverFactory, ConnectionString, TopicName, SubscriptionName);
        }

        [Test]
        public async Task ReceiveMessageBatchAsync_ReceivesBatchOfMessages()
        {
            const int batchSize = 10;
            const int prefetchCount = 123;

            await serviceBusMessageReceiver.ReceiveMessageBatchAsync(batchSize, prefetchCount);

            await messageReceiver.Received().ReceiveAsync(batchSize);
        }

        [Test]
        public async Task ReceiveMessageBatchAsync_SetsPrefetchCount()
        {
            const int batchSize = 10;
            const int prefetchCount = 123;

            await serviceBusMessageReceiver.ReceiveMessageBatchAsync(batchSize, prefetchCount);

            messageReceiver.PrefetchCount.Should().Be(prefetchCount);
        }

        [Test]
        public async Task ReceiveMessageBatchAsync_WhenNoMessagesReceived_ReturnsEmptyCollection()
        {
            var messages = await serviceBusMessageReceiver.ReceiveMessageBatchAsync(1, 2);

            messages.Should().BeEmpty();
        }

        [Test]
        public async Task RenewMessageLockAsync_RenewsLock()
        {
            const string lockToken = "token";

            await serviceBusMessageReceiver.RenewMessageLockAsync(lockToken);

            await messageReceiver.Received().RenewLockAsync(lockToken);
        }

        [Test]
        public async Task CompleteMessageAsync_CompletesMessage()
        {
            const string lockToken = "token";

            await serviceBusMessageReceiver.CompleteMessageAsync(lockToken);

            await messageReceiver.Received().CompleteAsync(lockToken);
        }

        [Test]
        public async Task AbandonMessageAsync_AbandonsMessage()
        {
            const string lockToken = "token";

            await serviceBusMessageReceiver.AbandonMessageAsync(lockToken);

            await messageReceiver.Received().AbandonAsync(lockToken);
        }
    }
}
