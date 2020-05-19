using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Services.Scoring;
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
        public async Task<ScoringResult> Score(ScoringRequest scoringRequest)
        {
            return await scoringRequestService.Score(scoringRequest);
        }
    }
}