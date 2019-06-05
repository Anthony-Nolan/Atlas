using Nova.SearchAlgorithm.Client.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Client;

namespace Nova.SearchAlgorithm.Client
{
    public interface ISearchAlgorithmClient
    {
        Task<SearchResultSet> Search(SearchRequest searchRequestCreationModel);
        Task<ScoringResult> Score(ScoringRequest scoringRequest);
    }

    public class SearchAlgorithmClient : ClientBase, ISearchAlgorithmClient
    {
        public SearchAlgorithmClient(ClientSettings settings, ILogger logger) : base(settings, logger)
        {
        }

        public async Task<SearchResultSet> Search(SearchRequest searchRequestCreationModel)
        {
            var request = GetRequest(HttpMethod.Post, "search", body: searchRequestCreationModel);
            return await MakeRequestAsync<SearchResultSet>(request);
        }
        
        public async Task<ScoringResult> Score(ScoringRequest scoringRequest)
        {
            var request = GetRequest(HttpMethod.Post, "score", body: scoringRequest);
            return await MakeRequestAsync<ScoringResult>(request);
        }
    }
}