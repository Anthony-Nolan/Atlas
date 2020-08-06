using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.MatchCalculation;
using Atlas.MatchPrediction.Models;
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
                var genotypeMatchDetails = new GenotypeMatchDetails
                {
                    AvailableLoci = matchCalculationInput.AllowedLoci,
                    DonorGenotype = matchCalculationInput.DonorHla,
                    PatientGenotype = matchCalculationInput.PatientHla,
                    MatchCounts = await matchCalculatorService.CalculateMatchCounts(
                        matchCalculationInput.PatientHla,
                        matchCalculationInput.DonorHla,
                        matchCalculationInput.HlaNomenclatureVersion,
                        matchCalculationInput.AllowedLoci)
                }; 
                
                return new JsonResult(new MatchCalculationResponse
                {
                    MatchCounts = genotypeMatchDetails.MatchCounts,
                    IsTenOutOfTenMatch = genotypeMatchDetails.MismatchCount == 0
                });
            }
            catch (Exception exception)
            {
                return new BadRequestObjectResult(exception);
            }
        }
    }
}