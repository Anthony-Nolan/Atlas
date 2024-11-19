using System;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchDispatcher
    {
        Task<string> DispatchSearch(SearchRequest matchingRequest);
    }

    public class SearchDispatcher : ISearchDispatcher
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;

        public SearchDispatcher(ISearchServiceBusClient searchServiceBusClient)
        {
            this.searchServiceBusClient = searchServiceBusClient;
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
    }
}