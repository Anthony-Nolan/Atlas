﻿using System.IO;
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

            return new JsonResult(new GenotypeImputationResponse
            {
                HlaTyping = input.SubjectInfo.HlaTyping.ToPhenotypeInfo().PrettyPrint(),
                MatchPredictionParameters = input.MatchPredictionParameters,
                HaplotypeFrequencySet = frequencySet.ToClientHaplotypeFrequencySet(),
                GenotypeCount = imputedGenotypes.GenotypeLikelihoods.Count,
                SumOfLikelihoods = imputedGenotypes.SumOfLikelihoods,
                GenotypeLikelihoods = imputedGenotypes.GenotypeLikelihoods.ToSingleDelimitedString()
            });
        }
    }
}
