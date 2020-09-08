using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Test.Verification.Models;
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

            var results = await GetVerificationResults(request);

            if (results.IsNullOrEmpty())
            {
                throw new Exception($"No results found for run id: {request.VerificationRunId}, with mismatch count: {request.MismatchCount}.");
            }

            WriteToCsv(request, results);
        }

        private async Task<IReadOnlyCollection<VerificationResult>> GetVerificationResults(VerificationResultsRequest request)
        {
            var results = await resultsCompiler.CompileVerificationResults(request);
            return results
                .OrderBy(r => r.Probability)
                .ToList();
        }

        private static void WriteToCsv(VerificationResultsRequest request, IReadOnlyCollection<VerificationResult> results)
        {
            var filePath = $"{request.WriteDirectory}\\Results_VerId-{request.VerificationRunId}_MMCount-{request.MismatchCount}.csv";

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer);
            csv.WriteRecords(results);

            Debug.WriteLine($"Results successfully written to {filePath}");
        }
    }
}
