using System;
using AutoMapper;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Repositories.SearchRequests.AzureStorage
{
    internal static class SearchRequestExtensions
    {
        internal static SearchRequestTableEntity ToTableEntity(this SearchRequest searchRequest, IMapper mapper)
        {
            // todo: NOVA-762 - decide the partition and row key values - in the interm, using the search type and Random number, respectively
            return new SearchRequestTableEntity(searchRequest.SearchType.ToString(), new Random().Next().ToString())
            {
                SerialisedSearchRequest = JsonConvert.SerializeObject(mapper.Map<SearchRequest>(searchRequest)),
            };
        }

        internal static SearchRequest ToSearchRequest(this SearchRequestTableEntity result, IMapper mapper)
        {
            var savedDonorSelection = mapper.Map<SearchRequest>(DeserializeSearchRequest(result.SerialisedSearchRequest));
            return savedDonorSelection;
        }

        private static SearchRequest DeserializeSearchRequest(string serialisedSearchRequest)
        {
            return JsonConvert.DeserializeObject<SearchRequest>(serialisedSearchRequest);
        }
    }
}