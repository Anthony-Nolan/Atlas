using System;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using FluentAssertions;
using FluentAssertions.Extensions;
using FluentValidation;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using SearchRequest = Atlas.Client.Models.Search.Requests.SearchRequest;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchDispatcherTests
    {
        private ISearchServiceBusClient searchServiceBusClient;
        private ISearchTrackingServiceBusClient searchTrackingServiceBusClient;

        private SearchDispatcher searchDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient, searchTrackingServiceBusClient);
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
    }
}