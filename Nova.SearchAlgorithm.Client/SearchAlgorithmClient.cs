using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using Nova.Utils.WebApi.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Client
{
    public interface ISearchAlgorithmClient
    {
        Task<int> CreateSearchRequest(SearchRequest searchRequest);
    }

    public class SearchAlgorithmClient : ClientBase, ISearchAlgorithmClient
    {
        public SearchAlgorithmClient(string baseUrl, string apiKey, JsonSerializerSettings settings)
            : base(baseUrl, apiKey, "search_algorithm_client", settings)
        {
        }

        public async Task<int> CreateSearchRequest(SearchRequest searchRequest)
        {
            var request = GetRequest(HttpMethod.Post, "search/create-search-request", body: searchRequest);
            return await MakeRequestAsync<int>(request);
        }
    }
}