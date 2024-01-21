using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.Verification.Compilation;
using CsvHelper;
using MoreLinq.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

            var results = await CompileResults(request);
            WriteResults(request, results);

            System.Diagnostics.Debug.WriteLine("Completed writing results.");
        }

        private async Task<IReadOnlyCollection<VerificationResult>> CompileResults(VerificationResultsRequest request)
        {
            var singleLocusRequests = MatchPredictionStaticData.MatchPredictionLoci
                .SelectMany(l => BuildCompileRequestByPrediction(request.VerificationRunId, l));
            var crossLociRequests = BuildCompileRequestByPrediction(request.VerificationRunId);
            var compileRequests = singleLocusRequests.Concat(crossLociRequests);

            var results = new List<VerificationResult>();

            foreach (var compileRequest in compileRequests)
            {
                System.Diagnostics.Debug.WriteLine($"Compiling results for {compileRequest}.");
                results.Add(await resultsCompiler.CompileVerificationResults(compileRequest));
            }

            return results;
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

        private static void WriteResults(VerificationResultsRequest request, IReadOnlyCollection<VerificationResult> results)
        {
            var avEDir = $"{request.WriteDirectory}\\AvE";
            Directory.CreateDirectory(avEDir);

            results.ForEach(r => WriteActualVsExpectedResults(avEDir, request.VerificationRunId, r));
            WriteMetrics(request.WriteDirectory, request.VerificationRunId, results);
        }

        private static void WriteActualVsExpectedResults(string writeDirectory, int runId, VerificationResult result)
        {
            if (result.ActualVersusExpectedResults.IsNullOrEmpty())
            {
                System.Diagnostics.Debug.WriteLine($"No results found for {result.Request}.");
                return;
            }

            var filePath = $"{writeDirectory}\\VerId-{runId}" + $"_Prediction-{result.Request.PredictionName}.csv";

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer);
            csv.WriteRecords(result.ActualVersusExpectedResults.OrderBy(r => r.Probability));

            System.Diagnostics.Debug.WriteLine($"AvE results written for {result.Request}.");
        }

        private static void WriteMetrics(string writeDirectory, int runId, IReadOnlyCollection<VerificationResult> results)
        {
            var filePath = $"{writeDirectory}\\VerId-{runId}-metrics.csv";

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer);
            csv.WriteRecords(results.Select(r => new
            {
                RunId = r.Request.VerificationRunId,
                Locus = r.Request.LocusName,
                MM = r.Request.MismatchCount,
                r.TotalPdpCount,
                WCBD = r.WeightedCityBlockDistance,
                r.WeightedLinearRegression.Slope,
                r.WeightedLinearRegression.Intercept
            }));
        }
    }
}
