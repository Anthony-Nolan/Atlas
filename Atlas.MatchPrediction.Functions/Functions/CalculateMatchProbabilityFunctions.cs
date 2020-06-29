using System.IO;
using System.Threading.Tasks;
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
        private readonly IMatchProbabilityService matchProbabilityService;

        public CalculateMatchProbabilityFunctions(IMatchProbabilityService matchProbabilityService)
        {
            this.matchProbabilityService = matchProbabilityService;
        }

        [FunctionName(nameof(CalculateZeroMismatchProbability))]
        public async Task<IActionResult> CalculateZeroMismatchProbability(
            [HttpTrigger(AuthorizationLevel.Function, "post")] [RequestBodyType(typeof(MatchProbabilityInput), "match probability")]
            HttpRequest request)
        {
            var matchProbabilityInput = JsonConvert.DeserializeObject<MatchProbabilityInput>(await new StreamReader(request.Body).ReadToEndAsync());

            var matchProbabilityResponse = await matchProbabilityService.CalculateMatchProbability(matchProbabilityInput);
            return new JsonResult(matchProbabilityResponse);
        }
    }
}
