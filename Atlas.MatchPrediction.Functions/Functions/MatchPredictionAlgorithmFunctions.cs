using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.MatchProbability;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class CalculateMatchProbabilityFunctions
    {
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;

        public CalculateMatchProbabilityFunctions(IMatchPredictionAlgorithm matchPredictionAlgorithm)
        {
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
        }

        [FunctionName(nameof(CalculateMatchProbability))]
        public async Task<IActionResult> CalculateMatchProbability(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(SingleDonorMatchProbabilityInput), nameof(SingleDonorMatchProbabilityInput))]
            HttpRequest request)
        {
            try
            {
                var matchProbabilityInput =
                    JsonConvert.DeserializeObject<SingleDonorMatchProbabilityInput>(await new StreamReader(request.Body).ReadToEndAsync());

                var matchProbabilityResponse = await matchPredictionAlgorithm.RunMatchPredictionAlgorithm(matchProbabilityInput);
                return new JsonResult(matchProbabilityResponse);
            }
            catch (Exception exception)
            {
                return new BadRequestObjectResult(exception);
            }
        }

        [FunctionName(nameof(CalculateMatchProbabilityBatch))]
        public async Task<IActionResult> CalculateMatchProbabilityBatch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(MultipleDonorMatchProbabilityInput), nameof(MultipleDonorMatchProbabilityInput))]
            HttpRequest request)
        {
            try
            {
                var matchProbabilityInput =
                    JsonConvert.DeserializeObject<MultipleDonorMatchProbabilityInput>(await new StreamReader(request.Body).ReadToEndAsync());
                var matchProbabilityResponse = await matchPredictionAlgorithm.RunMatchPredictionAlgorithmBatch(matchProbabilityInput);
                return new JsonResult(matchProbabilityResponse);
            }
            catch (Exception exception)
            {
                return new BadRequestObjectResult(exception);
            }
        }
    }
}