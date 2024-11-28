using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.MatchingAlgorithm.Clients.Scoring
{
    public interface IMatchingAlgorithmScoringFunctionsClient
    {
        Task<ScoringResult> Score(DonorHlaScoringRequest request);

        Task<IEnumerable<DonorScoringResult>> ScoreBatch(BatchScoringRequest request);
    }

    public class MatchingAlgorithmScoringFunctionsClient : MatchingAlgorithmHttpFunctionClient, IMatchingAlgorithmScoringFunctionsClient
    {
        public MatchingAlgorithmScoringFunctionsClient(HttpClient client) : base(client)
        {
        }

        public async Task<ScoringResult> Score(DonorHlaScoringRequest request)
        {
            return await PostRequest<DonorHlaScoringRequest, ScoringResult>("Score", request);
        }

        public async Task<IEnumerable<DonorScoringResult>> ScoreBatch(BatchScoringRequest request)
        {
            return await PostRequest<BatchScoringRequest, IEnumerable<DonorScoringResult>>("api/ScoreBatch", request);
        }
    }
}