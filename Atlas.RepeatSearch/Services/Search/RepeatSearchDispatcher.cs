using Atlas.Client.Models.Search.Requests;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Models;
using Atlas.MatchingAlgorithm.Validators.SearchRequest;
using FluentValidation;

namespace Atlas.RepeatSearch.Services.Search
{
    public interface IRepeatSearchDispatcher
    {
        Task<string> DispatchSearch(RepeatSearchRequest matchingRequest);
    }

    public class RepeatSearchDispatcher : IRepeatSearchDispatcher
    {
        private readonly IRepeatSearchServiceBusClient repeatSearchServiceBusClient;

        public RepeatSearchDispatcher(IRepeatSearchServiceBusClient repeatSearchServiceBusClient)
        {
            this.repeatSearchServiceBusClient = repeatSearchServiceBusClient;
        }

        /// <returns>A unique identifier for the dispatched search request</returns>
        public async Task<string> DispatchSearch(RepeatSearchRequest matchingRequest)
        {
            await new SearchRequestValidator().ValidateAndThrowAsync(matchingRequest.SearchRequest);
            var repeatSearchRequestId = Guid.NewGuid().ToString();

            var identifiedRepeatSearchRequest = new IdentifiedRepeatSearchRequest
            {
                RepeatSearchRequest = matchingRequest,
                RepeatSearchId = repeatSearchRequestId
            };
            await repeatSearchServiceBusClient.PublishToRepeatSearchRequestsTopic(identifiedRepeatSearchRequest);

            return repeatSearchRequestId;
        }
    }
}
