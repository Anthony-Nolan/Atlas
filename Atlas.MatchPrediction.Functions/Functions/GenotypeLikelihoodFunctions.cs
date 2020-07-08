using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
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
        private readonly IFrequencySetService frequencySetService;

        public GenotypeLikelihoodFunctions(IGenotypeLikelihoodService genotypeLikelihoodService, IFrequencySetService frequencySetService)
        {
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.frequencySetService = frequencySetService;
        }

        [FunctionName(nameof(CalculateGenotypeLikelihood))]
        public async Task<IActionResult> CalculateGenotypeLikelihood(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(GenotypeLikelihoodInput), nameof(GenotypeLikelihoodInput))]
            HttpRequest request)
        {
            var genotypeLikelihood = JsonConvert.DeserializeObject<GenotypeLikelihoodInput>(await new StreamReader(request.Body).ReadToEndAsync());
            genotypeLikelihood.FrequencySetMetaData ??= new FrequencySetMetadata();

            var frequencySet = await frequencySetService.GetSingleHaplotypeFrequencySet(genotypeLikelihood.FrequencySetMetaData);

            var likelihood = await genotypeLikelihoodService.CalculateLikelihood(genotypeLikelihood.Genotype, frequencySet);
            return new JsonResult(new GenotypeLikelihoodResponse { Likelihood = likelihood });
        }
    }
}