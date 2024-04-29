using System.IO;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Utils.Http;
using Atlas.Debug.Client.Models.MatchPrediction;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Functions.Services.Debug;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.MatchPrediction.Functions.Functions.Debug
{
    public class MatchCalculationFunctions
    {
        private readonly IHaplotypeFrequencyService frequencyService;
        private readonly IGenotypeMatcher genotypeMatcher;

        public MatchCalculationFunctions(
            IHaplotypeFrequencyService frequencyService,
            IGenotypeMatcher genotypeMatcher)
        {
            this.frequencyService = frequencyService;
            this.genotypeMatcher = genotypeMatcher;
        }

        [FunctionName(nameof(MatchPatientDonorGenotypes))]
        public async Task<IActionResult> MatchPatientDonorGenotypes(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = $"{RouteConstants.DebugRoutePrefix}/{nameof(MatchPatientDonorGenotypes)}")]
            [RequestBodyType(typeof(GenotypeMatcherRequest), nameof(GenotypeMatcherRequest))]
            HttpRequest request)
        {
            var input = JsonConvert.DeserializeObject<GenotypeMatcherRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            var frequencySet = await frequencyService.GetHaplotypeFrequencySets(
                input.Donor.FrequencySetMetadata,
                input.Patient.FrequencySetMetadata);

            var result = await genotypeMatcher.MatchPatientDonorGenotypes(new GenotypeMatcherInput
            {
                PatientData = new SubjectData(input.Patient.HlaTyping.ToPhenotypeInfo(), new SubjectFrequencySet(frequencySet.PatientSet, "debug-patient")),
                DonorData = new SubjectData(input.Donor.HlaTyping.ToPhenotypeInfo(), new SubjectFrequencySet(frequencySet.DonorSet, "debug-donor")),
                MatchPredictionParameters = input.MatchPredictionParameters
            });

            var response = new GenotypeMatcherResponse
            {
                MatchPredictionParameters = input.MatchPredictionParameters,
                PatientInfo = BuildSubjectResult(result.PatientResult, frequencySet.PatientSet, input.Patient),
                DonorInfo = BuildSubjectResult(result.DonorResult, frequencySet.DonorSet, input.Donor),
                MatchedGenotypePairs = result.GenotypeMatchDetails.ToSingleDelimitedString()
            };

            return new JsonResult(response);
        }

        private static SubjectResult BuildSubjectResult(
            GenotypeMatcherResult.SubjectResult subjectResult, 
            HaplotypeFrequencySet set, 
            SubjectInfo subjectInfo)
        {
            return new SubjectResult(
                subjectResult.IsUnrepresented,
                subjectResult.GenotypeCount,
                subjectResult.SumOfLikelihoods,
                set, 
                subjectInfo.HlaTyping.ToPhenotypeInfo().PrettyPrint());
        }
    }
}