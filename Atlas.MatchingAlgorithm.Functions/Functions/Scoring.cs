using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Services.Scoring;
using Atlas.Utils.CodeAnalysis;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class Scoring
    {
        private readonly IScoringRequestService scoringRequestService;

        public Scoring(IScoringRequestService scoringRequestService)
        {
            this.scoringRequestService = scoringRequestService;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName("Score")]
        public async Task<ScoringResult> Score([HttpTrigger] HttpRequest httpRequest)
        {
            var scoringRequest = JsonConvert.DeserializeObject<ScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            return await scoringRequestService.Score(scoringRequest);
        }
    }
}