using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class GenotypeLikelihoodFunctions
    {
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;

        public GenotypeLikelihoodFunctions(IGenotypeLikelihoodService genotypeLikelihoodService)
        {
            this.genotypeLikelihoodService = genotypeLikelihoodService;
        }

        [FunctionName(nameof(CalculateGenotypeLikelihood))]
        public async Task<IActionResult> CalculateGenotypeLikelihood(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(GenotypeLikelihoodInput), nameof(GenotypeLikelihoodInput))]
            HttpRequest request)
        {
            var genotypeLikelihood = JsonConvert.DeserializeObject<GenotypeLikelihoodInput>(await new StreamReader(request.Body).ReadToEndAsync());
            genotypeLikelihood.FrequencySetMetadata ??= new FrequencySetMetadata();
            
            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotypeLikelihood.Genotype, genotypeLikelihood.FrequencySetMetadata);
            return new JsonResult(new GenotypeLikelihoodResponse { Likelihood = likelihood });
        }
    }
}