using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Functions.Models.Debug;
using Atlas.MatchPrediction.Functions.Services.Debug;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using MoreLinq;
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

        [FunctionName(nameof(Impute))]
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
                AllowedMatchPredictionLoci = input.AllowedLoci.ToHashSet()
            });

            return new JsonResult(new GenotypeImputationResponse
            {
                HlaTyping = input.SubjectInfo.HlaTyping.ToPhenotypeInfo().PrettyPrint(),
                AllowedLoci = input.AllowedLoci,
                HaplotypeFrequencySet = frequencySet,
                GenotypeCount = imputedGenotypes.GenotypeLikelihoods.Count,
                GenotypeLikelihoods = imputedGenotypes.GenotypeLikelihoods.ToSingleDelimitedString()
            });
        }
    }
}
