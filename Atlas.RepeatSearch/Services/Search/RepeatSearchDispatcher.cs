using Atlas.Client.Models.Search.Requests;
using System;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Validators;
using Atlas.SearchTracking.Common.Clients;
using FluentValidation;
using Atlas.SearchTracking.Common.Enums;
using Atlas.SearchTracking.Common.Models;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Helpers;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchDispatcher
    {
        Task<string> DispatchSearch(RepeatSearchRequest matchingRequest);

        Task DispatchSearchTrackingEvent(RepeatSearchRequest repeatSearchRequest, string id);
    }

    public class RepeatSearchDispatcher : IRepeatSearchDispatcher
    {
        private readonly IRepeatSearchServiceBusClient repeatSearchServiceBusClient;
        private readonly ISearchTrackingServiceBusClient searchTrackingServiceBusClient;

        public RepeatSearchDispatcher(IRepeatSearchServiceBusClient repeatSearchServiceBusClient, ISearchTrackingServiceBusClient searchTrackingServiceBusClient)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
            this.searchTrackingServiceBusClient = searchTrackingServiceBusClient;
        }

        /// <returns>A unique identifier for the dispatched search request</returns>
        public async Task<string> DispatchSearch(RepeatSearchRequest matchingRequest)
        {
            await new RepeatSearchRequestValidator().ValidateAndThrowAsync(matchingRequest);
            var repeatSearchRequestId = Guid.NewGuid().ToString();

            var identifiedRepeatSearchRequest = new IdentifiedRepeatSearchRequest
            {
                OriginalSearchId = matchingRequest.OriginalSearchId,
                RepeatSearchRequest = matchingRequest,
                RepeatSearchId = repeatSearchRequestId
            };
            await repeatSearchServiceBusClient.PublishToRepeatSearchRequestsTopic(identifiedRepeatSearchRequest);

            return repeatSearchRequestId;
        }

        public async Task DispatchSearchTrackingEvent(RepeatSearchRequest repeatSearchRequest, string id)
        {
            var searchRequestedEvent = new SearchRequestedEvent
            {
                SearchRequestId = new Guid(id),
                IsRepeatSearch = true,
                OriginalSearchRequestId = new Guid(repeatSearchRequest.OriginalSearchId),
                RepeatSearchCutOffDate = repeatSearchRequest.SearchCutoffDate.Value.UtcDateTime,
                RequestJson = JsonConvert.SerializeObject(repeatSearchRequest),
                SearchCriteria = SearchTrackingEventHelper.GetSearchCriteria(repeatSearchRequest.SearchRequest),
                DonorType = repeatSearchRequest.SearchRequest.SearchDonorType.ToString(),
                RequestTimeUtc = DateTime.UtcNow
            };

            await searchTrackingServiceBusClient.PublishSearchTrackingEvent(searchRequestedEvent, SearchTrackingEventType.SearchRequested);
        }
    }
}
