using System.Globalization;
using System.Reflection;
using Atlas.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.ApplicationInsights;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoFixture;
using AwesomeAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.SearchTracking.Test.ApplicationInsights;

[TestFixture]
public class SearchTrackingEventLoggerExtensionsTests
{
    private Fixture fixture = null!;
    private IAtlasLogger logger = null!;
    private Dictionary<string, string>? sentProperties;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        logger = Substitute.For<IAtlasLogger>();

        sentProperties = null;
        logger.When(l => l.SendEvent(Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>()))
            .Do(call => sentProperties = call.ArgAt<Dictionary<string, string>>(2));
    }

    [Test]
    public void SendSearchTrackingEvent_SendsEventNamedAfterEventTypeAtInfoLevel()
    {
        var eventType = fixture.Create<SearchTrackingEventType>();

        logger.SendSearchTrackingEvent(fixture.Create<SearchRequestedEvent>(), eventType);

        logger.Received(1).SendEvent(eventType.ToString(), LogLevel.Info, Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
    }

    [Test]
    public void SendSearchTrackingEvent_SendsOnePropertyForEachFieldWithoutPatientInformation()
    {
        logger.SendSearchTrackingEvent(fixture.Create<SearchRequestedEvent>(), SearchTrackingEventType.SearchRequested);

        sentProperties!.Keys.Should().BeEquivalentTo(typeof(SearchRequestedEvent).GetProperties()
            .Where(property => !property.IsDefined(typeof(PatientInformationAttribute)))
            .Select(property => property.Name));
    }

    [Test]
    public void SendSearchTrackingEvent_SendsValuesAsTheServiceBusMessageHasThem()
    {
        var searchRequestedEvent = fixture.Build<SearchRequestedEvent>()
            .With(e => e.RequestTimeUtc, fixture.Create<DateTime>().ToUniversalTime())
            .Create();

        logger.SendSearchTrackingEvent(searchRequestedEvent, SearchTrackingEventType.SearchRequested);

        sentProperties![nameof(SearchRequestedEvent.SearchIdentifier)].Should().Be(searchRequestedEvent.SearchIdentifier.ToString());
        sentProperties[nameof(SearchRequestedEvent.IsRepeatSearch)].Should().Be(searchRequestedEvent.IsRepeatSearch.ToString().ToLowerInvariant());
        DateTime.Parse(sentProperties[nameof(SearchRequestedEvent.RequestTimeUtc)], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .Should().Be(searchRequestedEvent.RequestTimeUtc).And.BeIn(DateTimeKind.Utc);
        sentProperties[nameof(SearchRequestedEvent.SearchCriteria)].Should().Be(searchRequestedEvent.SearchCriteria);
        JsonConvert.DeserializeObject<List<string>>(sentProperties[nameof(SearchRequestedEvent.DonorRegistryCodes)])
            .Should().Equal(searchRequestedEvent.DonorRegistryCodes);
    }

    [Test]
    public void SendSearchTrackingEvent_LeavesOutNullFields()
    {
        var searchRequestedEvent = fixture.Build<SearchRequestedEvent>()
            .Without(e => e.OriginalSearchIdentifier)
            .Without(e => e.RepeatSearchCutOffDate)
            .Create();

        logger.SendSearchTrackingEvent(searchRequestedEvent, SearchTrackingEventType.SearchRequested);

        sentProperties.Should().NotContainKeys(
            nameof(SearchRequestedEvent.OriginalSearchIdentifier),
            nameof(SearchRequestedEvent.RepeatSearchCutOffDate));
    }

    [Test]
    public void SendSearchTrackingEvent_LeavesOutRequestJson()
    {
        logger.SendSearchTrackingEvent(fixture.Create<SearchRequestedEvent>(), SearchTrackingEventType.SearchRequested);

        sentProperties.Should().NotContainKey(nameof(SearchRequestedEvent.RequestJson));
    }

    [Test]
    public void SendSearchTrackingEvent_SendsFieldsOfNestedObjectsWithDottedNames()
    {
        logger.SendSearchTrackingEvent(fixture.Create<MatchingAlgorithmCompletedEvent>(), SearchTrackingEventType.MatchingAlgorithmCompleted);

        sentProperties!.Keys.Should().BeEquivalentTo(
            "SearchIdentifier",
            "OriginalSearchIdentifier",
            "AttemptNumber",
            "CompletionTimeUtc",
            "CompletionDetails.IsSuccessful",
            "CompletionDetails.FailureInfo.Type",
            "CompletionDetails.TotalAttemptsNumber",
            "CompletionDetails.NumberOfResults",
            "CompletionDetails.RepeatSearchResultsDetails.AddedResultCount",
            "CompletionDetails.RepeatSearchResultsDetails.RemovedResultCount",
            "CompletionDetails.RepeatSearchResultsDetails.UpdatedResultCount",
            "HlaNomenclatureVersion",
            "ResultsSent",
            "ResultsSentTimeUtc");
    }

    [Test]
    public void SendSearchTrackingEvent_SendsEnumValuesAsNames()
    {
        var completedEvent = fixture.Create<MatchingAlgorithmCompletedEvent>();

        logger.SendSearchTrackingEvent(completedEvent, SearchTrackingEventType.MatchingAlgorithmCompleted);

        sentProperties.Should().Contain("CompletionDetails.FailureInfo.Type", completedEvent.CompletionDetails.FailureInfo!.Type!.Value.ToString());
    }

    [Test]
    public void SendSearchTrackingEvent_LeavesOutFailureMessageAndStacktrace()
    {
        logger.SendSearchTrackingEvent(fixture.Create<MatchingAlgorithmCompletedEvent>(), SearchTrackingEventType.MatchingAlgorithmCompleted);

        sentProperties.Should().ContainKey("CompletionDetails.FailureInfo.Type")
            .And.NotContainKeys("CompletionDetails.FailureInfo.Message", "CompletionDetails.FailureInfo.ExceptionStacktrace");
    }
}
