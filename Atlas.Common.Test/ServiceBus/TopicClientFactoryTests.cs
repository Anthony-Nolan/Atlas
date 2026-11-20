using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using AutoFixture;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Common.Test.ServiceBus;

[TestFixture]
public class TopicClientFactoryTests
{
    private const int ConcurrentCallers = 8;

    // Long enough that the other callers ask for the topic while its first sender is still being made.
    private static readonly TimeSpan CreateSenderDuration = TimeSpan.FromMilliseconds(50);

    private Fixture fixture;
    private ServiceBusClient serviceBusClient;
    private TopicClientFactory factory;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        serviceBusClient = Substitute.For<ServiceBusClient>();
        serviceBusClient.CreateSender(Arg.Any<string>()).Returns(_ => Substitute.For<ServiceBusSender>());
        factory = new TopicClientFactory(serviceBusClient);
    }

    [Test]
    public void BuildTopicClient_ForSameTopic_ReturnsSameClient()
    {
        var topicName = fixture.Create<string>();

        var first = factory.BuildTopicClient(topicName);
        var second = factory.BuildTopicClient(topicName);

        second.Should().BeSameAs(first);
        serviceBusClient.Received(1).CreateSender(topicName);
    }

    [Test]
    public void BuildTopicClient_ForDifferentTopics_ReturnsDifferentClients()
    {
        var first = factory.BuildTopicClient(fixture.Create<string>());
        var second = factory.BuildTopicClient(fixture.Create<string>());

        second.Should().NotBeSameAs(first);
    }

    [Test]
    public void BuildTopicClient_CalledConcurrentlyForNewTopic_CreatesOneSender()
    {
        var topicName = fixture.Create<string>();
        serviceBusClient.CreateSender(topicName).Returns(_ =>
        {
            Thread.Sleep(CreateSenderDuration);
            return Substitute.For<ServiceBusSender>();
        });
        var clients = new ConcurrentBag<ITopicClient>();

        Parallel.For(0, ConcurrentCallers, _ => clients.Add(factory.BuildTopicClient(topicName)));

        clients.Distinct().Should().ContainSingle();
        serviceBusClient.Received(1).CreateSender(topicName);
    }

    [Test]
    public async Task DisposeAsync_OnBuiltClient_DoesNotCloseSharedSender()
    {
        var topicName = fixture.Create<string>();
        var sender = Substitute.For<ServiceBusSender>();
        serviceBusClient.CreateSender(topicName).Returns(sender);
        var message = new ServiceBusMessage(fixture.Create<string>());

        await factory.BuildTopicClient(topicName).DisposeAsync();
        await factory.BuildTopicClient(topicName).SendAsync(message);

        await sender.DidNotReceive().DisposeAsync();
        await sender.DidNotReceive().CloseAsync(Arg.Any<CancellationToken>());
        await sender.Received(1).SendMessageAsync(message, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisposeAsync_OnFactory_ClosesEverySender()
    {
        var senders = fixture.CreateMany<string>(2).ToDictionary(topicName => topicName, _ => Substitute.For<ServiceBusSender>());
        foreach (var (topicName, sender) in senders)
        {
            serviceBusClient.CreateSender(topicName).Returns(sender);
            factory.BuildTopicClient(topicName);
        }

        await factory.DisposeAsync();

        foreach (var sender in senders.Values)
        {
            await sender.Received(1).DisposeAsync();
        }
    }
}
