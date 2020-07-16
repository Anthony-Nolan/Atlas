using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class ScoringFunctions
    {
        private readonly IScoringRequestService scoringRequestService;

        public ScoringFunctions(IScoringRequestService scoringRequestService)
        {
            this.scoringRequestService = scoringRequestService;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(Score))]
        public async Task<ScoringResult> Score([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
        {
            var scoringRequest = JsonConvert.DeserializeObject<DonorHlaScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            return await scoringRequestService.Score(scoringRequest);
        }
    }
}