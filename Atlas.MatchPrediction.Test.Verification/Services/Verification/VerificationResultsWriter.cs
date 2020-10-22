using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation;
using CsvHelper;

namespace Atlas.MatchPrediction.Test.Verification.Services.Verification
{
    public interface IVerificationResultsWriter
    {
        Task WriteVerificationResultsToFile(VerificationResultsRequest request);
    }

    internal class VerificationResultsWriter : IVerificationResultsWriter
    {
        private readonly IVerificationResultsCompiler resultsCompiler;

        public VerificationResultsWriter(IVerificationResultsCompiler resultsCompiler)
        {
            this.resultsCompiler = resultsCompiler;
        }

        public async Task WriteVerificationResultsToFile(VerificationResultsRequest request)
        {
            if (request.WriteDirectory.IsNullOrEmpty())
            {
                throw new ArgumentException($"{nameof(request.WriteDirectory)} cannot be null or empty; provide a valid directory.");
            }

            await CompileAndWriteResults(request);

            Debug.WriteLine("Completed writing results.");
        }

        private async Task CompileAndWriteResults(VerificationResultsRequest request)
        {
            var singleLocusRequests = MatchPredictionStaticData.MatchPredictionLoci
                .SelectMany(l => BuildCompileRequestByPrediction(request.VerificationRunId, l));
            var crossLociRequests = BuildCompileRequestByPrediction(request.VerificationRunId);
            var compileRequests = singleLocusRequests.Concat(crossLociRequests);

            foreach (var compileRequest in compileRequests)
            {
                await FetchAndWriteResults(request, compileRequest);
            }
        }

        private static IEnumerable<CompileResultsRequest> BuildCompileRequestByPrediction(int runId, Locus? locus = null)
        {
            var mismatchCounts = new[] { 0, 1, 2 };
            return mismatchCounts.Select(mc => new CompileResultsRequest
            {
                VerificationRunId = runId,
                Locus = locus,
                MismatchCount = mc
            });
        }

        private async Task FetchAndWriteResults(VerificationResultsRequest request, CompileResultsRequest compileRequest)
        {
            Debug.WriteLine($"Compiling results for {compileRequest}...");

            var results = await resultsCompiler.CompileVerificationResults(compileRequest);

            if (results.ActualVersusExpectedResults.IsNullOrEmpty())
            {
                Debug.WriteLine($"No results found for {compileRequest}.");
            }
            else
            {
                WriteActualVsExpectedResultsToCsv(request, compileRequest, results.ActualVersusExpectedResults);
                Debug.WriteLine($"Results written for {compileRequest}.");
            }
        }

        private static void WriteActualVsExpectedResultsToCsv(
            VerificationResultsRequest request,
            CompileResultsRequest compileRequest,
            IEnumerable<ActualVersusExpectedResult> results)
        {
            var writeDir = $"{request.WriteDirectory}\\AvE";
            Directory.CreateDirectory(writeDir);

            var filePath = $"{writeDir}\\VerId-{request.VerificationRunId}" +
                           $"_MMCount-{compileRequest.MismatchCount}_{compileRequest.PredictionName}.csv";

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer);
            csv.WriteRecords(results.OrderBy(r => r.Probability));
        }
    }
}
