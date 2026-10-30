using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using AutoFixture;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using FluentValidation;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using SearchRequest = Atlas.Client.Models.Search.Requests.SearchRequest;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchDispatcherTests
    {
        private ISearchServiceBusClient searchServiceBusClient;
        private ISearchTrackingServiceBusClient searchTrackingServiceBusClient;
        private IAtlasLogger logger;
        private Fixture fixture;

        private SearchDispatcher searchDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();
            logger = Substitute.For<IAtlasLogger>();
            fixture = new Fixture();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient, searchTrackingServiceBusClient, logger);
        }

        [Test]
        public async Task DispatchSearch_DispatchesSearchWithId()
        {
            await searchDispatcher.DispatchSearch(
                new SearchRequestBuilder()
                    .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                    .WithTotalMismatchCount(0)
                    .Build());

            await searchServiceBusClient.Received().PublishToSearchRequestsTopic(Arg.Is<IdentifiedSearchRequest>(r => r.Id != null));
        }

        [Test]
        public void DispatchSearch_ValidatesSearchRequest()
        {
            var invalidSearchRequest = new SearchRequest();

            Assert.ThrowsAsync<ValidationException>(() => searchDispatcher.DispatchSearch(invalidSearchRequest));
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenSearchRequested_DispatchesEventWithSearchRequested()
        {
            var id = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            var searchRequest = new SearchRequestBuilder()
                .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                .WithTotalMismatchCount(0)
                .WithBetterMatchesConfig(true)
                .WithMatchPredictionConfig(true)
                .WithDonorRegistryCodes(["A", "B"])
                .Build();

            SearchRequestedEvent actualSearchRequestedEvent = null;

            var expectedSearchRequestedEvent = SearchRequestedEventBuilder.New.Build();
            expectedSearchRequestedEvent.RequestJson = JsonConvert.SerializeObject(searchRequest);

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<SearchRequestedEvent>(x => actualSearchRequestedEvent = x),
                Arg.Is(SearchTrackingEventType.SearchRequested));

            await searchDispatcher.DispatchSearchTrackingEvent(searchRequest, id);

            actualSearchRequestedEvent.Should().BeEquivalentTo(expectedSearchRequestedEvent, options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, 1.Seconds())).WhenTypeIs<DateTime>();
                return options;
            });
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenSearchRequested_SendsSearchRequestedEventToApplicationInsights()
        {
            var id = fixture.Create<Guid>().ToString();
            var searchRequest = new SearchRequestBuilder()
                .WithSearchHla(new PhenotypeInfo<string>(fixture.Create<string>()))
                .WithTotalMismatchCount(0)
                .Build();

            Dictionary<string, string> sentProperties = null;
            logger.When(l => l.SendEvent(
                    nameof(SearchTrackingEventType.SearchRequested),
                    LogLevel.Info,
                    Arg.Any<Dictionary<string, string>>(),
                    Arg.Any<Dictionary<string, double>>()))
                .Do(call => sentProperties = call.ArgAt<Dictionary<string, string>>(2));

            await searchDispatcher.DispatchSearchTrackingEvent(searchRequest, id);

            sentProperties.Should().Contain(nameof(SearchRequestedEvent.SearchIdentifier), id)
                .And.Contain(nameof(SearchRequestedEvent.IsRepeatSearch), "false")
                .And.NotContainKeys(nameof(SearchRequestedEvent.OriginalSearchIdentifier), nameof(SearchRequestedEvent.RepeatSearchCutOffDate))
                .And.NotContainKey(nameof(SearchRequestedEvent.RequestJson));
        }

        [Test]
        public async Task DispatchSearchTrackingEvent_WhenServiceBusPublishFails_StillSendsEventToApplicationInsights()
        {
            var searchRequest = new SearchRequestBuilder()
                .WithSearchHla(new PhenotypeInfo<string>(fixture.Create<string>()))
                .WithTotalMismatchCount(0)
                .Build();
            searchTrackingServiceBusClient
                .PublishSearchTrackingEvent(Arg.Any<SearchRequestedEvent>(), Arg.Any<SearchTrackingEventType>())
                .ThrowsAsync(fixture.Create<Exception>());

            var act = () => searchDispatcher.DispatchSearchTrackingEvent(searchRequest, fixture.Create<Guid>().ToString());

            await act.Should().ThrowAsync<Exception>();
            logger.Received(1).SendEvent(
                nameof(SearchTrackingEventType.SearchRequested),
                LogLevel.Info,
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<Dictionary<string, double>>());
        }
    }
}