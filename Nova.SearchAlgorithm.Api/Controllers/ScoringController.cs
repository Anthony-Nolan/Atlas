using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services.Scoring;

namespace Nova.SearchAlgorithm.Api.Controllers
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