using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Atlas.MatchPrediction.Client.Models.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class ExpandAmbiguousPhenotypeFunctions
    {
        private readonly IExpandAmbiguousPhenotypeService expandAmbiguousPhenotypeService;

        public ExpandAmbiguousPhenotypeFunctions(IExpandAmbiguousPhenotypeService expandAmbiguousPhenotypeService)
        {
            this.expandAmbiguousPhenotypeService = expandAmbiguousPhenotypeService;
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
                var genotypes = await expandAmbiguousPhenotypeService.ExpandPhenotype(
                    expandAmbiguousPhenotypeInput.Phenotype,
                    expandAmbiguousPhenotypeInput.HlaNomenclatureVersion);

                return new JsonResult(new ExpandAmbiguousPhenotypeResponse {Genotypes = genotypes});
            }
            catch (Exception)
            {
                return new InternalServerErrorResult();
            }
        }
    }
}