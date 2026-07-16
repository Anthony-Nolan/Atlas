using System.Text;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.Functions.Models;
using Atlas.Functions.Services;
using Atlas.Functions.Settings;
using AutoFixture;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.Functions.Test.Services;

[TestFixture]
internal class SearchCompletionMessageSenderTests
{
    private const string SearchResultsTopic = "search-results-ready";

    private ITopicClientFactory topicClientFactory;
    private ITopicClient topicClient;
    private SearchCompletionMessageSender sender;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        topicClient = Substitute.For<ITopicClient>();
        topicClientFactory = Substitute.For<ITopicClientFactory>();
        topicClientFactory.BuildTopicClient(Arg.Any<string>()).Returns(topicClient);
        var logger = Substitute.For<ISearchLogger<SearchLoggingContext>>();

        sender = new SearchCompletionMessageSender(
            Options.Create(new MessagingServiceBusSettings
            {
                SearchResultsTopic = SearchResultsTopic,
                RepeatSearchResultsTopic = "repeat-search-results-ready",
                SendRetryCount = 1,
                SendRetryCooldownSeconds = 0
            }),
            logger,
            topicClientFactory);
    }

    [TearDown]
    public async Task TearDown()
    {
        await topicClient.DisposeAsync();
    }

    [Test]
    public async Task PublishFailureMessage_SendsUnsuccessfulNotificationCarryingFailureDetail()
    {
        var parameters = fixture.Build<SendFailureNotificationParameters>()
            .Without(p => p.RepeatSearchRequestId)
            .Create();

        await sender.PublishFailureMessage(parameters);

        topicClientFactory.Received(1).BuildTopicClient(SearchResultsTopic);
        var notification = CapturedNotification();
        notification.WasSuccessful.Should().BeFalse();
        notification.SearchRequestId.Should().Be(parameters.SearchRequestId);
        notification.FailureInfo.StageReached.Should().Be(parameters.StageReached);
        notification.FailureInfo.FailureDetail.Should().Be(parameters.FailureDetail);
        notification.FailureInfo.MatchingAlgorithmFailureInfo.Should().BeEquivalentTo(parameters.MatchingAlgorithmFailureInfo);
    }

    private SearchResultsNotification CapturedNotification()
    {
        var sentMessage = topicClient.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(ITopicClient.SendAsync))
            .GetArguments()
            .OfType<ServiceBusMessage>()
            .Single();
        var json = Encoding.UTF8.GetString(sentMessage.Body.ToArray());
        return JsonConvert.DeserializeObject<SearchResultsNotification>(json)!;
    }
}
