using System;
using System.IO;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchPredictionSteps.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.ExpandAmbiguousPhenotype;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
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
        private readonly IHaplotypeFrequencyService haplotypeFrequencyService;

        public ExpandAmbiguousPhenotypeFunctions(
            ICompressedPhenotypeExpander compressedPhenotypeExpander,
            IHaplotypeFrequencyService haplotypeFrequencyService)
        {
            this.compressedPhenotypeExpander = compressedPhenotypeExpander;
            this.haplotypeFrequencyService = haplotypeFrequencyService;
        }

        [FunctionName(nameof(ExpandAmbiguousPhenotype))]
        public async Task<IActionResult> ExpandAmbiguousPhenotype(
            [HttpTrigger(AuthorizationLevel.Function, "post")] [RequestBodyType(typeof(ExpandAmbiguousPhenotypeInput), "phenotype input")]
            HttpRequest request)
        {
            var expandAmbiguousPhenotypeInput =
                JsonConvert.DeserializeObject<ExpandAmbiguousPhenotypeInput>(await new StreamReader(request.Body)
                    .ReadToEndAsync());
            try
            {
                var set = await haplotypeFrequencyService.GetSingleHaplotypeFrequencySet(expandAmbiguousPhenotypeInput.FrequencySetMetadata);
                var haplotypes = (await haplotypeFrequencyService.GetAllHaplotypeFrequencies(set.Id)).Keys;
                var genotypes = await compressedPhenotypeExpander.ExpandCompressedPhenotype(
                    expandAmbiguousPhenotypeInput.Phenotype,
                    expandAmbiguousPhenotypeInput.HlaNomenclatureVersion,
                    expandAmbiguousPhenotypeInput.AllowedLoci, haplotypes);

                return new JsonResult(new ExpandAmbiguousPhenotypeResponse {Genotypes = genotypes});
            }
            catch (Exception exception)
            {
                return new BadRequestObjectResult(exception);
            }
        }
    }
}