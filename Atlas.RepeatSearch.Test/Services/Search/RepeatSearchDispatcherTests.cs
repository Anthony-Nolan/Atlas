using System;
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
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using FluentAssertions;
using Newtonsoft.Json;

namespace Atlas.RepeatSearch.Test.Services.Search
{
    [TestFixture]
    public class RepeatSearchDispatcherTests
    {
        private IRepeatSearchServiceBusClient repeatSearchServiceBusClient;
        private ISearchTrackingServiceBusClient searchTrackingServiceBusClient;

        private RepeatSearchDispatcher repeatSearchDispatcher;

        [SetUp]
        public void SetUp()
        {
            repeatSearchServiceBusClient = Substitute.For<IRepeatSearchServiceBusClient>();
            searchTrackingServiceBusClient = Substitute.For<ISearchTrackingServiceBusClient>();

            repeatSearchDispatcher = new RepeatSearchDispatcher(repeatSearchServiceBusClient, searchTrackingServiceBusClient);
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
                SearchCutoffDate = new DateTimeOffset(DateTime.Today, new TimeSpan(1, 0, 0)),
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
            var searchRequest = new SearchRequestBuilder()
                .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                .WithTotalMismatchCount(0)
                .Build();

            var repeatSearchRequest = new RepeatSearchRequest()
            {
                OriginalSearchId = "11111111-2222-3333-4444-555555555555",
                SearchCutoffDate = new DateTimeOffset(DateTime.Today, new TimeSpan(1, 0, 0)),
                SearchRequest = searchRequest
            };

            SearchRequestedEvent actualSearchRequestedEvent = null;

            var expectedSearchRequestedEvent = new SearchRequestedEvent
            {
                SearchRequestId = new Guid(id),
                IsRepeatSearch = true,
                OriginalSearchRequestId = new Guid(repeatSearchRequest.OriginalSearchId),
                RepeatSearchCutOffDate = repeatSearchRequest.SearchCutoffDate.Value.UtcDateTime,
                RequestJson = JsonConvert.SerializeObject(repeatSearchRequest),
                SearchCriteria = SearchTrackingEventHelper.GetSearchCriteria(repeatSearchRequest.SearchRequest),
                DonorType = searchRequest.SearchDonorType.ToString(),
                RequestTimeUtc = new DateTime(2024, 07, 24)
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(
                Arg.Do<SearchRequestedEvent>(x => actualSearchRequestedEvent = x),
                Arg.Is(SearchTrackingEventType.SearchRequested));

            await repeatSearchDispatcher.DispatchSearchTrackingEvent(repeatSearchRequest, id);

            actualSearchRequestedEvent.Should().BeEquivalentTo(expectedSearchRequestedEvent,
                x => x.Excluding(s => s.RequestTimeUtc));
        }
    }
}
