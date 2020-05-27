using System.IO;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Functions.Models;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class GenotypeLikelihoodFunction
    {
        [FunctionName(nameof(CalculateGenotypeLikelihood))]
        public async Task<IActionResult> CalculateGenotypeLikelihood([HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PhenotypeInfo<string>), "genotype info")] HttpRequest request)
        {
            var genotype = JsonConvert.DeserializeObject<PhenotypeInfo<string>>(await new StreamReader(request.Body).ReadToEndAsync());

            return new JsonResult(new GenotypeLikelihoodResponse() { Likelihood = 1 });
        }
    }
}
