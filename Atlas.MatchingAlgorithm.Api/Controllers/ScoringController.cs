using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Microsoft.AspNetCore.Mvc;

namespace Atlas.MatchingAlgorithm.Api.Controllers
{
    public class ScoringController : ControllerBase
    {
        private readonly IScoringRequestService scoringRequestService;

        public ScoringController(IScoringRequestService scoringRequestService)
        {
            this.scoringRequestService = scoringRequestService;
        }

        [HttpPost]
        [Route("score")]
        public async Task<ScoringResult> Score(ScoringRequest<PhenotypeInfo<string>> scoringRequest)
        {
            return await scoringRequestService.Score(scoringRequest);
        }
    }
}