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
        private readonly IWmdaDiscrepantAlleleResultsReporter alleleResultsReporter;
        private readonly IWmdaDiscrepantAntigenResultsReporter antigenResultsReporter;
        private readonly IWmdaDiscrepantResultsWriter resultsWriter;

        public WmdaConsensusDatasetFunctions(
            IScoreRequestProcessor scoreRequestProcessor,
            IWmdaDiscrepantAlleleResultsReporter alleleResultsReporter, 
            IWmdaDiscrepantAntigenResultsReporter antigenResultsReporter,
            IWmdaDiscrepantResultsWriter resultsWriter)
        {
            this.scoreRequestProcessor = scoreRequestProcessor;
            this.alleleResultsReporter = alleleResultsReporter;
            this.antigenResultsReporter = antigenResultsReporter;
            this.resultsWriter = resultsWriter;
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

        /// <summary>
        /// Report discrepant total mismatches - can be used on results from both exercise 1 and 2.
        /// </summary>
        [FunctionName(nameof(ReportDiscrepantResults_TotalMismatches))]
        public async Task ReportDiscrepantResults_TotalMismatches(
            [RequestBodyType(typeof(ReportDiscrepanciesRequest), nameof(ReportDiscrepanciesRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var reportRequest = JsonConvert.DeserializeObject<ReportDiscrepanciesRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var report = await alleleResultsReporter.ReportDiscrepantResults(reportRequest);
            await resultsWriter.WriteToFile(BuildDiscrepantResultsFilePath(reportRequest.ResultsFilePath, "total"), report);
        }

        /// <summary>
        /// Report discrepant antigen mismatches - can only be used on results from exercise 2.
        /// </summary>
        [FunctionName(nameof(ReportDiscrepantResults_AntigenMismatches))]
        public async Task ReportDiscrepantResults_AntigenMismatches(
            [RequestBodyType(typeof(ReportDiscrepanciesRequest), nameof(ReportDiscrepanciesRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var reportRequest = JsonConvert.DeserializeObject<ReportDiscrepanciesRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            var report = await antigenResultsReporter.ReportDiscrepantResults(reportRequest);
            await resultsWriter.WriteToFile(BuildDiscrepantResultsFilePath(reportRequest.ResultsFilePath, "antigen"), report);
        }

        private static ScoringCriteria BuildThreeLocusScoringCriteria()
        {
            return new ScoringCriteria
            {
                LociToScore = new[] { Locus.A, Locus.B, Locus.Drb1 },
                LociToExcludeFromAggregateScore = new List<Locus>()
            };
        }

        private static string BuildDiscrepantResultsFilePath(string resultsPath, string mismatchCountKeyword)
        {
            return 
                $"{Path.GetDirectoryName(resultsPath)}/" +
                $"{Path.GetFileNameWithoutExtension(resultsPath)}-{mismatchCountKeyword}-discrepancies.txt";
        }
    }
}