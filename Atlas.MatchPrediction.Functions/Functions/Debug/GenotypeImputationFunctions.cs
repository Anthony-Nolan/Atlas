using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.MatchPrediction;
using Atlas.MatchPrediction.Functions.Services.Debug;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions.Debug
{
    public class GenotypeImputationFunctions
    {
        private readonly IGenotypeImputationService genotypeImputationService;
        private readonly IHaplotypeFrequencyService frequencyService;

        public GenotypeImputationFunctions(
            IGenotypeImputationService genotypeImputationService,
            IHaplotypeFrequencyService frequencyService)
        {
            this.genotypeImputationService = genotypeImputationService;
            this.frequencyService = frequencyService;
        }

        [Function(nameof(Impute))]
        public async Task<IActionResult> Impute(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(Impute)}")]
            [RequestBodyType(typeof(GenotypeImputationRequest), nameof(GenotypeImputationRequest))]
            HttpRequest request)
        {
            var input = JsonConvert.DeserializeObject<GenotypeImputationRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            input.SubjectInfo.FrequencySetMetadata ??= new FrequencySetMetadata();

            var frequencySet = await frequencyService.GetSingleHaplotypeFrequencySet(input.SubjectInfo.FrequencySetMetadata);

            var imputedGenotypes = await genotypeImputationService.Impute(new ImputationInput
            {
                SubjectData = new SubjectData(input.SubjectInfo.HlaTyping.ToPhenotypeInfo(), new SubjectFrequencySet(frequencySet, "debug-subject")),
                MatchPredictionParameters = input.MatchPredictionParameters
            });

            // The result carries each kept genotype together with its name form and likelihood, so the name-keyed view
            // this debug response shows is projected here rather than held in the result. One entry per surviving name
            // form: two genotypes differing only in typing category share one.
            var likelihoodsByName = imputedGenotypes.Genotypes
                .GroupBy(genotype => genotype.Names)
                .ToDictionary(group => group.Key, group => group.First().Likelihood);

            return new JsonResult(new GenotypeImputationResponse
            {
                HlaTyping = input.SubjectInfo.HlaTyping.ToPhenotypeInfo().PrettyPrint(),
                MatchPredictionParameters = input.MatchPredictionParameters,
                HaplotypeFrequencySet = frequencySet.ToClientHaplotypeFrequencySet(),
                GenotypeCount = likelihoodsByName.Count,
                SumOfLikelihoods = imputedGenotypes.SumOfLikelihoods,
                GenotypeLikelihoods = likelihoodsByName.ToSingleDelimitedString()
            });
        }
    }
}
