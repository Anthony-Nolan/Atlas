using System;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using FluentValidation;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchDispatcher
    {
        Task<string> DispatchSearch(SearchRequest matchingRequest);

        Task DispatchSearchTrackingEvent(SearchRequest searchRequest, string id);
    }

    public class SearchDispatcher : ISearchDispatcher
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;
        private readonly ISearchTrackingServiceBusClient searchTrackingServiceBusClient;

        public SearchDispatcher(ISearchServiceBusClient searchServiceBusClient, ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        {
            this.searchServiceBusClient = searchServiceBusClient;
            this.searchTrackingServiceBusClient = searchTrackingServiceBusClient;
        }

        /// <returns>A unique identifier for the dispatched search request</returns>
        public async Task<string> DispatchSearch(SearchRequest matchingRequest)
        {
            await new SearchRequestValidator().ValidateAndThrowAsync(matchingRequest);
            var searchRequestId = Guid.NewGuid().ToString();

            var identifiedSearchRequest = new IdentifiedSearchRequest
            {
                SearchRequest = matchingRequest,
                Id = searchRequestId
            };
            await searchServiceBusClient.PublishToSearchRequestsTopic(identifiedSearchRequest);

            return searchRequestId;
        }

        public async Task DispatchSearchTrackingEvent(SearchRequest searchRequest, string id)
        {
            var searchRequestedEvent = new SearchRequestedEvent
            {
                SearchRequestId = new Guid(id),
                IsRepeatSearch = false,
                OriginalSearchRequestId = null,
                RepeatSearchCutOffDate = null,
                RequestJson = JsonConvert.SerializeObject(searchRequest),
                SearchCriteria = SearchTrackingEventHelper.GetSearchCriteria(searchRequest),
                DonorType = searchRequest.SearchDonorType.ToString(),
                RequestTimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(searchRequestedEvent, SearchTrackingEventType.SearchRequested);
        }
    }
}