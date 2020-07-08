using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchCalculation;
using Atlas.MatchPrediction.Services.MatchCalculation;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class MatchCalculationFunctions
    {
        private readonly IMatchCalculationService matchCalculatorService;

        public MatchCalculationFunctions(IMatchCalculationService matchCalculatorService)
        {
            this.matchCalculatorService = matchCalculatorService;
        }

        [FunctionName(nameof(CalculateMatch))]
        public async Task<IActionResult> CalculateMatch(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(MatchCalculationInput), nameof(MatchCalculationInput))]
            HttpRequest request)
        {
            var matchCalculationInput = JsonConvert.DeserializeObject<MatchCalculationInput>(await new StreamReader(request.Body).ReadToEndAsync());

            try
            {
                var match = await matchCalculatorService.MatchAtPGroupLevel(
                    matchCalculationInput.PatientHla,
                    matchCalculationInput.DonorHla,
                    matchCalculationInput.HlaNomenclatureVersion);

                return new JsonResult(new MatchCalculationResponse {MatchCounts = match.MatchCounts, IsTenOutOfTenMatch = match.MismatchCount == 0});
            }
            catch (Exception exception)
            {
                return new BadRequestObjectResult(exception);
            }
        }
    }
}