using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Client.Models.MatchCalculation;
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

        [FunctionName(nameof(MatchAtGGroupLevelCalculation))]
        public async Task<IActionResult> MatchAtGGroupLevelCalculation(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(MatchCalculationInput), "match calculation")]
            HttpRequest request)
        {
            var matchCalculationInput = JsonConvert.DeserializeObject<MatchCalculationInput>(await new StreamReader(request.Body).ReadToEndAsync());

            var likelihood = await matchCalculatorService.MatchAtPGroupLevel(
                matchCalculationInput.PatientHla,
                matchCalculationInput.DonorHla,
                matchCalculationInput.HlaNomenclatureVersion);

            return new JsonResult(likelihood);
        }
    }
}