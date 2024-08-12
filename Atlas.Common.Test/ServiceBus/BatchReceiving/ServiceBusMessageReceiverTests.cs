using Atlas.Common.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
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

            messageReceiverFactory.GetMessageReceiver(TopicName, SubscriptionName, 6).Returns(messageReceiver);

            serviceBusMessageReceiver = new ServiceBusMessageReceiver<string>(messageReceiverFactory, TopicName, SubscriptionName, 6);
        }

        [Test]
        public async Task ReceiveMessageBatchAsync_ReceivesBatchOfMessages()
        {
            const int batchSize = 10;

            await serviceBusMessageReceiver.ReceiveMessageBatchAsync(batchSize);

            await messageReceiver.Received().ReceiveMessagesAsync(batchSize);
        }

        [Test]
        public async Task ReceiveMessageBatchAsync_SetsPrefetchCount()
        {
            const int batchSize = 10;
            const int prefetchCount = 123;

            var receiver = new ServiceBusMessageReceiver<string>(messageReceiverFactory, TopicName, SubscriptionName, prefetchCount);

            messageReceiverFactory.Received().GetMessageReceiver(TopicName, SubscriptionName, prefetchCount);
        }

        [Test]
        public async Task ReceiveMessageBatchAsync_WhenNoMessagesReceived_ReturnsEmptyCollection()
        {
            var messages = await serviceBusMessageReceiver.ReceiveMessageBatchAsync(1);

            messages.Should().BeEmpty();
        }

        [Test]
        public async Task RenewMessageLockAsync_RenewsLock()
        {
            var lockToken = new object();

            await serviceBusMessageReceiver.RenewMessageLockAsync(lockToken);

            await messageReceiver.Received().RenewMessageLockAsync(lockToken);
        }

        [Test]
        public async Task CompleteMessageAsync_CompletesMessage()
        {
            var lockToken = new object();

            await serviceBusMessageReceiver.CompleteMessageAsync(lockToken);

            await messageReceiver.Received().CompleteMessageAsync(lockToken);
        }

        [Test]
        public async Task AbandonMessageAsync_AbandonsMessage()
        {
            var lockToken = new object();

            await serviceBusMessageReceiver.AbandonMessageAsync(lockToken);

            await messageReceiver.Received().AbandonMessageAsync(lockToken);
        }
    }
}
