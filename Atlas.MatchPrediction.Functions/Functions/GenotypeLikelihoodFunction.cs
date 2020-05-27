using System;
using System.IO;
using System.Threading.Tasks;
using FluentValidation;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Client.Models.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
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
        private readonly IGenotypeLikelihoodService genotypeLikelihoodService;

        public GenotypeLikelihoodFunction(IGenotypeLikelihoodService genotypeLikelihoodService)
        {
            this.genotypeLikelihoodService = genotypeLikelihoodService;
        }

        [FunctionName(nameof(CalculateGenotypeLikelihood))]
        public async Task<IActionResult> CalculateGenotypeLikelihood([HttpTrigger(AuthorizationLevel.Function, "post")]
            [RequestBodyType(typeof(PhenotypeInfo<string>), "genotype info")] HttpRequest request)
        {
            var genotype = JsonConvert.DeserializeObject<PhenotypeInfo<string>>(await new StreamReader(request.Body).ReadToEndAsync());

            try
            {
                var likelihood = genotypeLikelihoodService.CalculateLikelihood(genotype);
                return new JsonResult(new GenotypeLikelihoodResponse() { Likelihood = likelihood });
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult(e);
            }

        }
    }
}
