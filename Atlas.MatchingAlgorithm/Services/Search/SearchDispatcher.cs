using System;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation;

namespace Atlas.MatchingAlgorithm.Services.Search
{
    public interface ISearchDispatcher
    {
        Task<string> DispatchSearch(SearchRequest searchRequest);
    }

    public class SearchDispatcher : ISearchDispatcher
    {
        private readonly ISearchServiceBusClient searchServiceBusClient;

        public SearchDispatcher(ISearchServiceBusClient searchServiceBusClient)
        {
            this.searchServiceBusClient = searchServiceBusClient;
        }

        /// <returns>A unique identifier for the dispatched search request</returns>
        public async Task<string> DispatchSearch(SearchRequest searchRequest)
        {
            new SearchRequestValidator().ValidateAndThrow(searchRequest);
            var searchRequestId = Guid.NewGuid().ToString();

            var identifiedSearchRequest = new IdentifiedSearchRequest
            {
                SearchRequest = searchRequest,
                Id = searchRequestId
            };
            await searchServiceBusClient.PublishToSearchQueue(identifiedSearchRequest);

            return searchRequestId;
        }
    }
}