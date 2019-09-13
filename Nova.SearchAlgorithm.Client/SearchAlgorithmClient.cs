using System.Net.Http;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.Utils.ServiceClient;

namespace Nova.SearchAlgorithm.Client
{
    public interface ISearchAlgorithmClient
    {
        Task<ScoringResult> Score(ScoringRequest scoringRequest);
        Task<SearchInitiationResponse> InitiateSearch(SearchRequest searchRequest);
    }

    public class SearchAlgorithmClient : FunctionsClientBase, ISearchAlgorithmClient
    {
        public SearchAlgorithmClient(INovaFunctionsHttpClient novaHttpClient) : base(novaHttpClient)
        {
        }

        public async Task<ScoringResult> Score(ScoringRequest scoringRequest)
        {
            var request = HttpClient.GetRequest(HttpMethod.Post, "api/Score", body: scoringRequest);
            return await HttpClient.MakeRequestAsync<ScoringResult>(request);
        }

        public async Task<SearchInitiationResponse> InitiateSearch(SearchRequest searchRequest)
        {
            var request = HttpClient.GetRequest(HttpMethod.Post, "api/InitiateSearch", body: searchRequest);
            return await HttpClient.MakeRequestAsync<SearchInitiationResponse>(request);
        }
    }
}