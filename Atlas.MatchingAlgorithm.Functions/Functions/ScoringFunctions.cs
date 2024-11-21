using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
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
        [Function(nameof(Score))]
        [ProducesResponseType(typeof(ScoringResult), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Score(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(DonorHlaScoringRequest), nameof(DonorHlaScoringRequest))]
            HttpRequest httpRequest)
        {
            var scoringRequest = JsonConvert.DeserializeObject<DonorHlaScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            return new JsonResult(await scoringRequestService.Score(scoringRequest));
        }

        [Function(nameof(ScoreBatch))]
        [ProducesResponseType(typeof(List<DonorScoringResult>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ScoreBatch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(BatchScoringRequest), nameof(BatchScoringRequest))]
            HttpRequest httpRequest)
        {
            var batchScoringRequest = JsonConvert.DeserializeObject<BatchScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            return new JsonResult(await scoringRequestService.ScoreBatch(batchScoringRequest));
        }
    }
}