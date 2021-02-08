using Atlas.Client.Models.Search.Requests;
using System;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Models;
using Atlas.RepeatSearch.Validators;
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
    }
}
