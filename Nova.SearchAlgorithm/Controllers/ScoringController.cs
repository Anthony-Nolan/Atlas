using System.Threading.Tasks;
using System.Web.Http;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Scoring;
using Nova.SearchAlgorithm.Services.Scoring;

namespace Nova.SearchAlgorithm.Controllers
{
    public class ScoringController : ApiController
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