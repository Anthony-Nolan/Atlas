using System;
using Atlas.Common.ServiceBus;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DonorManagement;

[TestFixture]
public class DonorUpdateMessageProcessorFactoryTests
{
    private const string Topic = "donor-updates";
    private const string SubscriptionForDbA = "subscription-a";
    private const string SubscriptionForDbB = "subscription-b";

    private IMessageReceiverFactory messageReceiverFactory;
    private IDonorUpdateMessageProcessorFactory messageProcessorFactory;

    [SetUp]
    public void SetUp()
    {
        messageReceiverFactory = Substitute.For<IMessageReceiverFactory>();
        messageProcessorFactory = new DonorUpdateMessageProcessorFactory(
            messageReceiverFactory,
            new DonorManagementSettings { Topic = Topic, SubscriptionForDbA = SubscriptionForDbA, SubscriptionForDbB = SubscriptionForDbB });
    }

    [TestCase(TransientDatabase.DatabaseA, SubscriptionForDbA)]
    [TestCase(TransientDatabase.DatabaseB, SubscriptionForDbB)]
    public void CreateMessageProcessor_ReceivesFromSubscriptionOfTargetDatabase(TransientDatabase targetDatabase, string expectedSubscription)
    {
        messageProcessorFactory.CreateMessageProcessor(targetDatabase);

        messageReceiverFactory.Received(1).CreateMessageReceiver(Arg.Any<string>(), Arg.Any<string>());
        messageReceiverFactory.Received(1).CreateMessageReceiver(Topic, expectedSubscription);
    }

    [Test]
    public void CreateMessageProcessor_ForUnknownDatabase_Throws()
    {
        var creating = () => messageProcessorFactory.CreateMessageProcessor((TransientDatabase)99);

        creating.Should().Throw<ArgumentOutOfRangeException>();
        messageReceiverFactory.DidNotReceiveWithAnyArgs().CreateMessageReceiver(default, default);
    }
}
