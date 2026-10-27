using Atlas.Common.ServiceBus;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.ServiceBus
{
    [TestFixture]
    public class MessageReceiverFactoryTests
    {
        private const string TopicName = "topic";
        private const string SubscriptionName = "subscription";

        private ServiceBusClient client;
        private IMessageReceiverFactory messageReceiverFactory;

        [SetUp]
        public void SetUp()
        {
            client = Substitute.For<ServiceBusClient>();
            messageReceiverFactory = new MessageReceiverFactory(client);
        }

        [Test]
        public void CreateMessageReceiver_ForSameSubscription_CreatesNewReceiverEachTime()
        {
            var first = messageReceiverFactory.CreateMessageReceiver(TopicName, SubscriptionName);
            var second = messageReceiverFactory.CreateMessageReceiver(TopicName, SubscriptionName);

            second.Should().NotBeSameAs(first);
            client.Received(2).CreateReceiver(TopicName, SubscriptionName);
        }
    }
}
