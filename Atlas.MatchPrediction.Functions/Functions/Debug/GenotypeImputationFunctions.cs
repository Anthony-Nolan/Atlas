using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Http;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Functions.Models.Debug;
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
            input.FrequencySetMetadata ??= new FrequencySetMetadata();

            var frequencySet = await frequencyService.GetSingleHaplotypeFrequencySet(input.FrequencySetMetadata);

            var imputedGenotypes = await genotypeImputationService.Impute(new ImputationInput
            {
                HlaTyping = input.HlaTyping.ToPhenotypeInfo(),
                AllowedMatchPredictionLoci = MoreEnumerable.ToHashSet(input.AllowedLoci),
                FrequencySet = frequencySet,
                SubjectLogDescription = "debug-subject"
            });

            var genotypes = imputedGenotypes.GenotypeLikelihoods
                .Select(x => $"{x.Key.PrettyPrint()} {x.Value}");

            return new JsonResult(new GenotypeImputationResponse
            {
                HaplotypeFrequencySet = frequencySet,
                GenotypeLikelihoods = genotypes
            });
        }
    }
}
