using System.IO;
using System.Threading.Tasks;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.WmdaConsensusResults.Scorers;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;

namespace Atlas.ManualTesting.Functions
{
    /// <summary>
    /// Functions that run the mismatch counting exercises of the WMDA consensus dataset.
    /// </summary>
    public class WmdaConsensusDatasetFunctions
    {
        private readonly IWmdaExerciseOneScorer scorerOne;
        private readonly IWmdaExerciseTwoScorer scorerTwo;

        public WmdaConsensusDatasetFunctions(
            IWmdaExerciseOneScorer scorerOne, 
            IWmdaExerciseTwoScorer scorerTwo)
        {
            this.scorerOne = scorerOne;
            this.scorerTwo = scorerTwo;
        }

        [Function(nameof(ProcessWmdaConsensusDataset_Exercise1))]
        public async Task ProcessWmdaConsensusDataset_Exercise1(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            await scorerOne.ProcessScoreRequest(importAndScoreRequest);
        }

        [Function(nameof(ProcessWmdaConsensusDataset_Exercise2))]
        public async Task ProcessWmdaConsensusDataset_Exercise2(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            await scorerTwo.ProcessScoreRequest(importAndScoreRequest);
        }
    }
}