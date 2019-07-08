using System.Net.Http;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Client;

namespace Nova.SearchAlgorithm.Client
{
    public interface ISearchAlgorithmClient
    {
        Task<ScoringResult> Score(ScoringRequest scoringRequest);
        Task<SearchInitiationResponse> InitiateSearch(SearchRequest searchRequest);
    }

    public class SearchAlgorithmClient : ClientBase, ISearchAlgorithmClient
    {
        public SearchAlgorithmClient(ClientSettings settings, ILogger logger) : base(settings, logger)
        {
        }

        public async Task<ScoringResult> Score(ScoringRequest scoringRequest)
        {
            var request = GetRequest(HttpMethod.Post, "api/Score", body: scoringRequest);
            return await MakeRequestAsync<ScoringResult>(request);
        }

        public async Task<SearchInitiationResponse> InitiateSearch(SearchRequest searchRequest)
        {
            var request = GetRequest(HttpMethod.Post, "api/InitiateSearch", body: searchRequest);
            return await MakeRequestAsync<SearchInitiationResponse>(request);
        }
    }
}