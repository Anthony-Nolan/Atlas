using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions.Debug
{
    public class GenotypeLikelihoodFunctions
    {
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;
        private readonly IHaplotypeFrequencyService frequencyService;

        public GenotypeLikelihoodFunctions(IGenotypeLikelihoodService genotypeLikelihoodService, IHaplotypeFrequencyService frequencyService)
        {
            this.genotypeLikelihoodService = genotypeLikelihoodService;
            this.frequencyService = frequencyService;
        }

        [FunctionName(nameof(CalculateGenotypeLikelihood))]
        public async Task<IActionResult> CalculateGenotypeLikelihood(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(CalculateGenotypeLikelihood)}")]
            [RequestBodyType(typeof(GenotypeLikelihoodInput), nameof(GenotypeLikelihoodInput))]
            HttpRequest request)
        {
            var genotypeLikelihood = JsonConvert.DeserializeObject<GenotypeLikelihoodInput>(await new StreamReader(request.Body).ReadToEndAsync());
            genotypeLikelihood.FrequencySetMetaData ??= new FrequencySetMetadata();

            var frequencySet = await frequencyService.GetSingleHaplotypeFrequencySet(genotypeLikelihood.FrequencySetMetaData);

            var likelihood = await genotypeLikelihoodService.CalculateLikelihoodForGenotype(genotypeLikelihood.Genotype, frequencySet, genotypeLikelihood.AllowedLoci);
            return new JsonResult(new GenotypeLikelihoodResponse { Likelihood = likelihood });
        }
    }
}