using System.IO;
using System.Threading.Tasks;
using Atlas.ManualTesting.Models;
using Atlas.ManualTesting.Services.WmdaConsensusResults;
using Atlas.ManualTesting.Services.WmdaConsensusResults.Scorers;
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
        private readonly IWmdaExerciseOneScorer scorerOne;
        private readonly IWmdaExerciseTwoScorer scorerTwo;
        private readonly IWmdaDiscrepantAlleleResultsReporter alleleResultsReporter;
        private readonly IWmdaDiscrepantAntigenResultsReporter antigenResultsReporter;
        private readonly IWmdaDiscrepantResultsWriter resultsWriter;

        public WmdaConsensusDatasetFunctions(
            IWmdaExerciseOneScorer scorerOne, 
            IWmdaExerciseTwoScorer scorerTwo,
            IWmdaDiscrepantAlleleResultsReporter alleleResultsReporter, 
            IWmdaDiscrepantAntigenResultsReporter antigenResultsReporter,
            IWmdaDiscrepantResultsWriter resultsWriter)
        {
            this.scorerOne = scorerOne;
            this.scorerTwo = scorerTwo;
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
            await scorerOne.ProcessScoreRequest(importAndScoreRequest);
        }

        [FunctionName(nameof(ProcessWmdaConsensusDataset_Exercise2))]
        public async Task ProcessWmdaConsensusDataset_Exercise2(
            [RequestBodyType(typeof(ImportAndScoreRequest), nameof(ImportAndScoreRequest))]
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest request)
        {
            var importAndScoreRequest = JsonConvert.DeserializeObject<ImportAndScoreRequest>(await new StreamReader(request.Body).ReadToEndAsync());
            await scorerTwo.ProcessScoreRequest(importAndScoreRequest);
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

        private static string BuildDiscrepantResultsFilePath(string resultsPath, string mismatchCountKeyword)
        {
            return 
                $"{Path.GetDirectoryName(resultsPath)}/" +
                $"{Path.GetFileNameWithoutExtension(resultsPath)}-{mismatchCountKeyword}-discrepancies.txt";
        }
    }
}