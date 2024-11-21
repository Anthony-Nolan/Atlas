using Atlas.Common.Utils;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using System.Collections.Generic;
using System.Net;
using Atlas.MatchingAlgorithm.Clients.Scoring;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Client.Models.Scoring.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.Functions.PublicApi.Functions
{
    public class ScoringFunctions
    {
        private readonly IMatchingAlgorithmScoringFunctionsClient scoringClient;
        private readonly IMapper mapper;

        public ScoringFunctions(IMatchingAlgorithmScoringFunctionsClient scoringClient, IMapper mapper)
        {
            this.scoringClient = scoringClient;
            this.mapper = mapper;
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
            var request = mapper.Map<MatchingAlgorithm.Client.Models.Scoring.DonorHlaScoringRequest>(scoringRequest);
            var response = await scoringClient.Score(request);

            return new JsonResult(mapper.Map<ScoringResult>(response));
        }

        [Function(nameof(ScoreBatch))]
        [ProducesResponseType(typeof(List<DonorScoringResult>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ScoreBatch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(DonorHlaBatchScoringRequest), nameof(DonorHlaBatchScoringRequest))]
            HttpRequest httpRequest)
        {
            var batchScoringRequest = JsonConvert.DeserializeObject<DonorHlaBatchScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            var request = mapper.Map<MatchingAlgorithm.Client.Models.Scoring.BatchScoringRequest>(batchScoringRequest);
            var response = await scoringClient.ScoreBatch(request);

            return new JsonResult(mapper.Map<List<DonorScoringResult>>(response));
        }
    }
}
