using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Services.Search;
using Atlas.SearchTracking.Common.Clients;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.RepeatSearch.Models;
using FluentValidation;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.RepeatSearch.Test.TestHelpers.Builders;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoFixture;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Newtonsoft.Json;
using NSubstitute.ExceptionExtensions;

namespace Atlas.RepeatSearch.Test.Services.Search
{
    [TestFixture]
    public class RepeatSearchDispatcherTests
    {
        private IRepeatSearchServiceBusClient repeatSearchServiceBusClient;
        private ISearchTrackingServiceBusClient searchTrackingServiceBusClient;
        private IAtlasLogger logger;
        private Fixture fixture;

        private RepeatSearchDispatcher repeatSearchDispatcher;

        [SetUp]
        public void SetUp()
        {
            repeatSearchServiceBusClient = Substitute.For<IRepeatSearchServiceBusClient>();
            searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();
            logger = Substitute.For<IAtlasLogger>();
            fixture = new Fixture();

            repeatSearchDispatcher = new RepeatSearchDispatcher(repeatSearchServiceBusClient, searchTrackingServiceBusClient, logger);
        }

        [Test]
        public async Task DispatchRepeatSearch_DispatchesRepeatSearchWithId()
        {
            var searchRequest = new SearchRequestBuilder()
                    .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                    .WithTotalMismatchCount(0)
                    .Build();

            var repeatSearchRequest = new RepeatSearchRequest()
            {
                OriginalSearchId = "1",
                SearchCutoffDate = new DateTime(2024, 5, 11, 14, 30, 45, 0, 0, DateTimeKind.Utc),
                SearchRequest = searchRequest
            };

            await repeatSearchDispatcher.DispatchSearch(repeatSearchRequest);

            await repeatSearchServiceBusClient.Received().PublishToRepeatSearchRequestsTopic(
                Arg.Is<IdentifiedRepeatSearchRequest>(r => r.RepeatSearchId != null));
        }

        [Test]
        public void DispatchRepeatSearch_ValidatesRepeatSearchRequest()
        {
            var invalidSearchRequest = new RepeatSearchRequest();

            Assert.ThrowsAsync<ValidationException>(() => repeatSearchDispatcher.DispatchSearch(invalidSearchRequest));
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenRepeatSearchRequested_DispatchesEventWithSearchRequested()
        {
            var id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var originalSearchId = "11111111-2222-3333-4444-555555555555";

            var searchRequest = new SearchRequestBuilder()
                .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                .WithTotalMismatchCount(0)
                .WithBetterMatchesConfig(true)
                .WithMatchPredictionConfig(true)
                .WithDonorRegistryCodes(["A", "B"])
                .Build();

            var repeatSearchRequest = new RepeatSearchRequest()
            {
                OriginalSearchId = originalSearchId,
                SearchCutoffDate = new DateTime(2024, 5 ,11, 14, 30, 45, 0, 0, DateTimeKind.Utc),
                SearchRequest = searchRequest
            };

            SearchRequestedEvent actualSearchRequestedEvent = null;

            var expectedSearchRequestedEvent = SearchRequestedEventBuilder.New.Build();
            expectedSearchRequestedEvent.RequestJson = JsonConvert.SerializeObject(repeatSearchRequest);

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<SearchRequestedEvent>(x => actualSearchRequestedEvent = x),
                Arg.Is(SearchTrackingEventType.SearchRequested));

            await repeatSearchDispatcher.DispatchSearchTrackingEvent(repeatSearchRequest, id);

            actualSearchRequestedEvent.Should().BeEquivalentTo(expectedSearchRequestedEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenRepeatSearchRequested_SendsSearchRequestedEventToApplicationInsights()
        {
            var id = fixture.Create<Guid>().ToString();
            var repeatSearchRequest = BuildRepeatSearchRequest();

            Dictionary<string, string> sentProperties = null;
            logger.When(l => l.SendEvent(
                    nameof(SearchTrackingEventType.SearchRequested),
                    LogLevel.Info,
                    Arg.Any<Dictionary<string, string>>(),
                    Arg.Any<Dictionary<string, double>>()))
                .Do(call => sentProperties = call.ArgAt<Dictionary<string, string>>(2));

            await repeatSearchDispatcher.DispatchSearchTrackingEvent(repeatSearchRequest, id);

            var sentCutOffDate = sentProperties.Should().Contain(nameof(SearchRequestedEvent.SearchIdentifier), id)
                .And.Contain(nameof(SearchRequestedEvent.IsRepeatSearch), "true")
                .And.Contain(nameof(SearchRequestedEvent.OriginalSearchIdentifier), repeatSearchRequest.OriginalSearchId)
                .And.NotContainKey(nameof(SearchRequestedEvent.RequestJson))
                .And.ContainKey(nameof(SearchRequestedEvent.RepeatSearchCutOffDate)).WhoseValue;
            DateTimeOffset.Parse(sentCutOffDate, CultureInfo.InvariantCulture).Should().Be(repeatSearchRequest.SearchCutoffDate);
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenServiceBusPublishFails_StillSendsEventToApplicationInsights()
        {
            searchTrackingServiceBusClient
                .PublishSearchTrackingEvent(Arg.Any<SearchRequestedEvent>(), Arg.Any<SearchTrackingEventType>())
                .ThrowsAsync(fixture.Create<Exception>());

            var act = () => repeatSearchDispatcher.DispatchSearchTrackingEvent(BuildRepeatSearchRequest(), fixture.Create<Guid>().ToString());

            await act.Should().ThrowAsync<Exception>();
            logger.Received(1).SendEvent(
                nameof(SearchTrackingEventType.SearchRequested),
                LogLevel.Info,
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>());
        }

        private RepeatSearchRequest BuildRepeatSearchRequest() =>
            new()
            {
                OriginalSearchId = fixture.Create<Guid>().ToString(),
                SearchCutoffDate = fixture.Create<DateTimeOffset>(),
                SearchRequest = new SearchRequestBuilder()
                    .WithSearchHla(new PhenotypeInfo<string>(fixture.Create<string>()))
                    .WithTotalMismatchCount(0)
                    .Build()
            };
    }
}
