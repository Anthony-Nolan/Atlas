using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Services.Scoring;

namespace Nova.SearchAlgorithm.Functions.Functions
{
    public class Scoring
    {
        private readonly IScoringRequestService scoringRequestService;

        public Scoring(IScoringRequestService scoringRequestService)
        {
            this.scoringRequestService = scoringRequestService;
        }

        [FunctionName("Score")]
        public async Task<ScoringResult> Score([HttpTrigger] HttpRequest httpRequest)
        {
            var scoringRequest = JsonConvert.DeserializeObject<ScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            return await scoringRequestService.Score(scoringRequest);
        }
    }
}