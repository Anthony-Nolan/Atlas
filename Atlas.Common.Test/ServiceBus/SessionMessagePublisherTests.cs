using Atlas.Common.ServiceBus;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.Common.Test.ServiceBus
{
    [TestFixture]
    public class SessionMessagePublisherTests
    {
        private const string TopicName = "topic";
        private const string SessionId = "session-id";

        private ITopicClientFactory topicClientFactory;
        private ITopicClient topicClient;

        public class TestMessage
        {
            public string Id { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            topicClient = Substitute.For<ITopicClient>();
            topicClientFactory = Substitute.For<ITopicClientFactory>();
            topicClientFactory.BuildTopicClient(TopicName).Returns(topicClient);
        }

        [Test]
        public async Task PublishWithSession_WithoutPropertiesSelector_DoesNotStampApplicationProperties()
        {
            var publisher = new SessionMessagePublisher<TestMessage>(topicClientFactory, TopicName, 1, 0,
                Substitute.For<ILogger<SessionMessagePublisher<TestMessage>>>());

            await publisher.PublishWithSession(new TestMessage { Id = "id-1" }, SessionId);

            await topicClient.Received(1).SendAsync(Arg.Is<ServiceBusMessage>(m =>
                m.SessionId == SessionId && m.ApplicationProperties.Count == 0));
        }

        [Test]
        public async Task PublishWithSession_WithPropertiesSelector_StampsSelectedProperties()
        {
            var publisher = new SessionMessagePublisher<TestMessage>(topicClientFactory, TopicName, 1, 0,
                Substitute.For<ILogger<SessionMessagePublisher<TestMessage>>>(),
                m => new Dictionary<string, object> { { nameof(TestMessage.Id), m.Id } });

            await publisher.PublishWithSession(new TestMessage { Id = "id-1" }, SessionId);

            var message = (ServiceBusMessage)topicClient.ReceivedCalls().Should().ContainSingle().Which.GetArguments()[0];
            message.SessionId.Should().Be(SessionId);
            message.ApplicationProperties.Should().Equal(new Dictionary<string, object> { { nameof(TestMessage.Id), "id-1" } });
        }
    }
}
