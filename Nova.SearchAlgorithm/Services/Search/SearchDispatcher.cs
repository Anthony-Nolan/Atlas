using System;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Services.Search
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
            var searchId = Guid.NewGuid().ToString();
            var identifiedSearchRequest = new IdentifiedSearchRequest
            {
                SearchRequest = searchRequest,
                Id = searchId,
            };

            await searchServiceBusClient.PublishToSearchQueue(identifiedSearchRequest);
            return searchId;
        }
    }
}