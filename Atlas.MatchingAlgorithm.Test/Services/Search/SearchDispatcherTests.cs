using System;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using FluentAssertions;
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
                .Build();

            SearchRequestedEvent actualSearchRequestedEvent = null;

            var expectedSearchRequestedEvent = new SearchRequestedEvent
            {
                SearchRequestId = new Guid(id),
                IsRepeatSearch = false,
                OriginalSearchRequestId = null,
                RepeatSearchCutOffDate = null,
                RequestJson = JsonConvert.SerializeObject(searchRequest),
                SearchCriteria = SearchTrackingEventHelper.GetSearchCriteria(searchRequest),
                DonorType = searchRequest.SearchDonorType.ToString(),
                RequestTimeUtc = new DateTime(2024, 07, 24)
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<SearchRequestedEvent>(x => actualSearchRequestedEvent = x),
                Arg.Is(SearchTrackingEventType.SearchRequested));

            await searchDispatcher.DispatchSearchTrackingEvent(searchRequest, id);

            actualSearchRequestedEvent.Should().BeEquivalentTo(expectedSearchRequestedEvent,
                x => x.Excluding(s => s.RequestTimeUtc));
        }
    }
}