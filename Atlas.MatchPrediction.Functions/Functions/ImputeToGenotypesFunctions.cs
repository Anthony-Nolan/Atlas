using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Client.Models.ImputeToGenotypes;
using Atlas.MatchPrediction.Services.ImputeToGenotypes;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class ImputeToGenotypesFunctions
    {
        private readonly IImputeToGenotypesService genotypeImputationService;

        public ImputeToGenotypesFunctions(
            IHlaMetadataDictionaryFactory metadataDictionaryFactory,
            IImputeToGenotypesService genotypeImputationService)
        {
            this.genotypeImputationService = genotypeImputationService;
        }

        [FunctionName(nameof(ImputeToGenotypes))]
        public async Task<IActionResult> ImputeToGenotypes(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(ImputeToGenotypesInput), "phenotype input")]
            HttpRequest request)
        {
            var genotypeImputationInput =
                JsonConvert.DeserializeObject<ImputeToGenotypesInput>(await new StreamReader(request.Body)
                    .ReadToEndAsync());

            try
            {
                var genotypes = await genotypeImputationService.ImputePhenotype(
                    genotypeImputationInput.Phenotype,
                    genotypeImputationInput.HlaNomenclatureVersion);

                return new JsonResult(new ImputeToGenotypesResponse {Genotypes = genotypes});
            }
            catch (Exception)
            {
                return new InternalServerErrorResult();
            }
        }
    }
}