using System;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Results;

namespace Atlas.Debug.Client.Clients
{
    public interface ISearchTrackingFunctionsClient : ICommonAtlasFunctions
    {
        Task<SearchResult> GetSearchRequestByIdentifier(Guid searchIdentifier);
    }  

    public class SearchTrackingFunctionsClient :HttpFunctionClient, ISearchTrackingFunctionsClient
    {
        public SearchTrackingFunctionsClient(HttpClient client) :base(client)
        {
        }

        public async Task<SearchResult> GetSearchRequestByIdentifier(Guid searchIdentifier)
        {
            return await PostRequest<Guid, SearchResult>("/debug/search-tracking-info/by-search-identifier/", searchIdentifier);
        }
    }
}
