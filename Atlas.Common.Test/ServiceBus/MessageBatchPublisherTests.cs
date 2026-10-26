using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.Common.Test.ServiceBus
{
    [TestFixture]
    public class MessageBatchPublisherTests
    {
        private const string TopicName = "topic";

        private ITopicClientFactory topicClientFactory;
        private ITopicClient topicClient;
        private List<ServiceBusMessage> sentMessages;

        public class TestMessage
        {
            public string Id { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            sentMessages = new List<ServiceBusMessage>();
            topicClient = Substitute.For<ITopicClient>();
            topicClient.CreateMessageBatchAsync()
                .Returns(_ => ServiceBusModelFactory.ServiceBusMessageBatch(long.MaxValue, sentMessages));
            topicClientFactory = Substitute.For<ITopicClientFactory>();
            topicClientFactory.BuildTopicClient(TopicName).Returns(topicClient);
        }

        [Test]
        public async Task BatchPublish_WithoutPropertiesSelector_DoesNotStampApplicationProperties()
        {
            var publisher = new MessageBatchPublisher<TestMessage>(topicClientFactory, TopicName, 1, 0, Substitute.For<IAtlasLogger>());

            await publisher.BatchPublish(new[] { new TestMessage { Id = "id-1" } });

            sentMessages.Should().ContainSingle().Which.ApplicationProperties.Should().BeEmpty();
        }

        [Test]
        public async Task BatchPublish_WithPropertiesSelector_StampsSelectedPropertiesOnEachMessage()
        {
            var publisher = new MessageBatchPublisher<TestMessage>(topicClientFactory, TopicName, 1, 0, Substitute.For<IAtlasLogger>(),
                m => new Dictionary<string, object> { { nameof(TestMessage.Id), m.Id } });

            await publisher.BatchPublish(new[] { new TestMessage { Id = "id-1" }, new TestMessage { Id = "id-2" } });

            sentMessages.Should().HaveCount(2);
            sentMessages[0].ApplicationProperties.Should().Equal(new Dictionary<string, object> { { nameof(TestMessage.Id), "id-1" } });
            sentMessages[1].ApplicationProperties.Should().Equal(new Dictionary<string, object> { { nameof(TestMessage.Id), "id-2" } });
        }

        [Test]
        public async Task BatchPublish_WithPropertiesSelector_DoesNotStampNullValues()
        {
            var publisher = new MessageBatchPublisher<TestMessage>(topicClientFactory, TopicName, 1, 0, Substitute.For<IAtlasLogger>(),
                m => new Dictionary<string, object> { { nameof(TestMessage.Id), m.Id }, { "Other", "value" } });

            await publisher.BatchPublish(new[] { new TestMessage { Id = null } });

            sentMessages.Should().ContainSingle().Which.ApplicationProperties.Should().Equal(new Dictionary<string, object> { { "Other", "value" } });
        }
    }
}
