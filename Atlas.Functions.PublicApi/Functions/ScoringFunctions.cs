using Atlas.Common.Utils;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Clients.Scoring;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Client.Models.Scoring.Results;

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
        [FunctionName(nameof(Score))]
        public async Task<ScoringResult> Score(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(DonorHlaScoringRequest), nameof(DonorHlaScoringRequest))]
            HttpRequest httpRequest)
        {
            var scoringRequest = JsonConvert.DeserializeObject<DonorHlaScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            var request = mapper.Map<MatchingAlgorithm.Client.Models.Scoring.DonorHlaScoringRequest>(scoringRequest);
            var response = await scoringClient.Score(request);

            return mapper.Map<ScoringResult>(response);
        }

        [FunctionName(nameof(ScoreBatch))]
        public async Task<List<DonorScoringResult>> ScoreBatch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(DonorHlaBatchScoringRequest), nameof(DonorHlaBatchScoringRequest))]
            HttpRequest httpRequest)
        {
            var batchScoringRequest = JsonConvert.DeserializeObject<DonorHlaBatchScoringRequest>(await new StreamReader(httpRequest.Body).ReadToEndAsync());
            var request = mapper.Map<MatchingAlgorithm.Client.Models.Scoring.BatchScoringRequest>(batchScoringRequest);
            var response = await scoringClient.ScoreBatch(request);

            return mapper.Map<List<DonorScoringResult>>(response);
        }
    }
}
