using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Functions.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class MatchPredictionFunctions
    {
        private readonly IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private readonly IMatchPredictionValidator matchPredictionValidator;
        private readonly IMatchPredictionRequestDispatcher requestDispatcher;

        public MatchPredictionFunctions(
            IMatchPredictionAlgorithm matchPredictionAlgorithm,
            IMatchPredictionValidator matchPredictionValidator,
            IMatchPredictionRequestDispatcher requestDispatcher)
        {
            this.matchPredictionAlgorithm = matchPredictionAlgorithm;
            this.matchPredictionValidator = matchPredictionValidator;
            this.requestDispatcher = requestDispatcher;
        }

        /// <summary>
        /// Submits a match prediction request for a single patient-donor pair without running a full search.
        /// </summary>
        /// <returns>Unique Id for the match prediction request.</returns>
        [FunctionName(nameof(RequestMatchPrediction))]
        public async Task<IActionResult> RequestMatchPrediction(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(MatchPredictionRequest), nameof(MatchPredictionRequest))]
            HttpRequest request)
        {
            var matchPredictionRequest = JsonConvert.DeserializeObject<MatchPredictionRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var input = matchPredictionRequest.ToSingleDonorMatchProbabilityInput();

            var validationResult = matchPredictionValidator.ValidateMatchProbabilityInput(input);
            if (!validationResult.IsValid)
            {
                return new BadRequestObjectResult(validationResult.Errors);
            }

            var id = await requestDispatcher.DispatchMatchPredictionRequest(input);
            return new JsonResult(new MatchPredictionInitiationResponse { MatchPredictionRequestId = id });
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