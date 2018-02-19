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
            : base(new ClientSettings { ApiKey = apiKey, BaseUrl = baseUrl, ClientName = "search_algorithm_client", JsonSettings = settings})
        {
        }

        public async Task<int?> CreateSearchRequest(SearchRequestCreationModel searchRequestCreationModel)
        {
            //todo: NOVA-761 - decide what kind of object to return
            var request = GetRequest(HttpMethod.Post, "search-requests", body: searchRequestCreationModel);
            return await MakeRequestAsync<int?>(request);
        }
    }
}