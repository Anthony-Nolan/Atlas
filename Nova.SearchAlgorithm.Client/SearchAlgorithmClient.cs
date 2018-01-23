using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using Nova.Utils.WebApi.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Client
{
    public interface ISearchAlgorithmClient
    {
        Task<int?> CreateSearchRequest(SearchRequestCreationModel searchRequestCreationModel);
    }

    public class SearchAlgorithmClient : ClientBase, ISearchAlgorithmClient
    {
        public SearchAlgorithmClient(string baseUrl, string apiKey, JsonSerializerSettings settings)
            : base(baseUrl, apiKey, "search_algorithm_client", settings)
        {
        }

        public async Task<int?> CreateSearchRequest(SearchRequestCreationModel searchRequestCreationModel)
        {
            var request = GetRequest(HttpMethod.Post, "search-algorithm/create-search-request", body: searchRequestCreationModel);
            return await MakeRequestAsync<int?>(request);
        }
    }
}