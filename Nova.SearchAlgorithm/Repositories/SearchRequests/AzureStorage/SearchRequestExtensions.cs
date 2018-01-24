using System;
using AutoMapper;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;

namespace Nova.SearchAlgorithm.Repositories.SearchRequests.AzureStorage
{
    internal static class SearchRequestExtensions
    {
        internal static SearchRequestTableEntity ToTableEntity(this SearchRequestCreationModel searchRequest, IMapper mapper)
        {
            // todo: decide the partition and row key values - in the interm, using the search type and Random number, respectively
            return new SearchRequestTableEntity(searchRequest.SearchType.ToString(), new Random().Next().ToString())
            {
                SerialisedSearchRequest = JsonConvert.SerializeObject(mapper.Map<SearchRequestCreationModel>(searchRequest)),
            };
        }

        internal static SearchRequestCreationModel ToSearchRequest(this SearchRequestTableEntity result, IMapper mapper)
        {
            var savedDonorSelection = mapper.Map<SearchRequestCreationModel>(DeserializeSearchRequest(result.SerialisedSearchRequest));
            return savedDonorSelection;
        }

        private static SearchRequestCreationModel DeserializeSearchRequest(string serialisedSearchRequest)
        {
            return JsonConvert.DeserializeObject<SearchRequestCreationModel>(serialisedSearchRequest);
        }
    }
}