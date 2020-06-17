using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Client.Models.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class ExpandAmbiguousPhenotypeFunctions
    {
        private readonly ICompressedPhenotypeExpander compressedPhenotypeExpander;

        public ExpandAmbiguousPhenotypeFunctions(ICompressedPhenotypeExpander compressedPhenotypeExpander)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
        }

        [FunctionName(nameof(ExpandAmbiguousPhenotype))]
        public async Task<IActionResult> ExpandAmbiguousPhenotype(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(ExpandAmbiguousPhenotypeInput), "phenotype input")]
            HttpRequest request)
        {
            var expandAmbiguousPhenotypeInput =
                JsonConvert.DeserializeObject<ExpandAmbiguousPhenotypeInput>(await new StreamReader(request.Body)
                    .ReadToEndAsync());
            try
            {
                var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    expandAmbiguousPhenotypeInput.Phenotype,
                    expandAmbiguousPhenotypeInput.HlaNomenclatureVersion);

                return new JsonResult(new ExpandAmbiguousPhenotypeResponse {Genotypes = genotypes});
            }
            catch (Exception exception)
            {
                return new BadRequestObjectResult(exception);
            }
        }

        [FunctionName(nameof(NumberOfPermutationsOfAmbiguousPhenotype))]
        public async Task<IActionResult> NumberOfPermutationsOfAmbiguousPhenotype(
            [HttpTrigger(AuthorizationLevel.Function, "post")] [RequestBodyType(typeof(ExpandAmbiguousPhenotypeInput), "phenotype input")]
            HttpRequest request)
        {
            var expandAmbiguousPhenotypeInput =
                JsonConvert.DeserializeObject<ExpandAmbiguousPhenotypeInput>(await new StreamReader(request.Body).ReadToEndAsync());

            var genotypeCount = await compressedPhenotypeExpander.CalculateNumberOfPermutations(
                expandAmbiguousPhenotypeInput.Phenotype,
                expandAmbiguousPhenotypeInput.HlaNomenclatureVersion);

            return new JsonResult(genotypeCount);
        }
    }
}