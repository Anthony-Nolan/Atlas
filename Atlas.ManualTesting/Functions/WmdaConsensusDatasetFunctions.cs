using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.Scoring;
using Atlas.ManualTesting.Services.WmdaConsensusResults;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Atlas.ManualTesting.Functions
{
    /// <summary>
    /// Functions that run the mismatch counting exercises of the WMDA consensus dataset.
    /// </summary>
    public class WmdaConsensusDatasetFunctions
    {
        private readonly IScoreRequestProcessor scoreRequestProcessor;
        private readonly IWmdaDiscrepantResultsReporter wmdaDiscrepantResultsReporter;

        public WmdaConsensusDatasetFunctions(
            IScoreRequestProcessor scoreRequestProcessor,
            IWmdaDiscrepantResultsReporter wmdaDiscrepantResultsReporter)
        {
            this.scoreRequestProcessor = scoreRequestProcessor;
            this.wmdaDiscrepantResultsReporter = wmdaDiscrepantResultsReporter;
        }

        [FunctionName(nameof(ProcessWmdaConsensusDataset_Exercise1))]
        public async Task ProcessWmdaConsensusDataset_Exercise1(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            await scoreRequestProcessor.ProcessScoreRequest(new ScoreRequestProcessorInput
            {
                ImportAndScoreRequest = importAndScoreRequest,
                ScoringCriteria = BuildThreeLocusScoringCriteria(),
                ResultTransformer = (patientId, donorId, result) => new WmdaConsensusResultsFile(patientId, donorId, result).ToString()
            });
        }

        [FunctionName(nameof(ProcessWmdaConsensusDataset_Exercise2))]
        public async Task ProcessWmdaConsensusDataset_Exercise2(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());

            await scoreRequestProcessor.ProcessScoreRequest(new ScoreRequestProcessorInput
            {
                ImportAndScoreRequest = importAndScoreRequest,
                ScoringCriteria = BuildThreeLocusScoringCriteria(),
                ResultTransformer = (patientId, donorId, result) => new WmdaConsensusResultsFileSetTwo(patientId, donorId, result).ToString()
            });
        }

        [FunctionName(nameof(ReportDiscrepantResults_Exercise1))]
        public async Task ReportDiscrepantResults_Exercise1(
            [RequestBodyType(typeof(ReportDiscrepanciesRequest), nameof(ReportDiscrepanciesRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndCompareRequest = JsonConvert.DeserializeObject<ReportDiscrepanciesRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            await wmdaDiscrepantResultsReporter.ReportDiscrepantResults(importAndCompareRequest);
        }

        private static ScoringCriteria BuildThreeLocusScoringCriteria()
        {
            return new ScoringCriteria
            {
                LociToScore = new[] { Locus.A, Locus.B, Locus.Drb1 },
                LociToExcludeFromAggregateScore = new List<Locus>()
            };
        }
    }
}