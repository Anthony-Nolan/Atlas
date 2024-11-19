using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Test.Performance.Models;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using CsvHelper;

namespace Atlas.MatchingAlgorithm.Test.Performance
{
    internal class Program
    {
        private static readonly string DefaultOutputDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)?.Replace("\\bin\\Debug", "");

        public static async Task Main(string[] args)
        {
            var outputFilePath = args.Length > 0 ? args[0] : null;
            
            var results = new List<TestOutput>();
            var testsRun = 0;
            
            foreach (var testInput in TestCases.TestInputs)
            {
                var testOutput = await RunSearch(testInput);
                results.Add(testOutput);
                testsRun++;
                Console.WriteLine($"Run {testsRun}/{TestCases.TestInputs.Count()} tests");
            }

            WriteResultsToCsv(results, outputFilePath);
        }

        private static async Task<TestOutput> RunSearch(TestInput testInput)
        {
            var searchRequest = BuildSearchRequest(testInput);
            var metrics = await SearchTimingService.TimeSearchRequest(searchRequest, testInput.AlgorithmInstanceInfo);
            return new TestOutput(testInput, metrics, TestCases.Notes);
        }

        private static SearchRequest BuildSearchRequest(TestInput testInput)
        {
            var searchRequestBuilder = new SearchRequestBuilder()
                .WithSearchHla(testInput.Hla);

            switch (testInput.SearchType)
            {
                case SearchType.SixOutOfSix:
                    searchRequestBuilder = searchRequestBuilder.WithTotalMismatchCount(0);
                    break;
                case SearchType.AMismatchThreeLocus:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(1)
                        .WithLocusMismatchCount(Locus.A, 1);
                    break;
                case SearchType.BMismatchThreeLocus:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(1)
                        .WithLocusMismatchCount(Locus.B, 1);
                    break;
                case SearchType.Drb1MismatchThreeLocus:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(1)
                        .WithLocusMismatchCount(Locus.Drb1, 1);
                    break;
                case SearchType.TenOutOfTen:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(0)
                        .WithLocusMismatchCount(Locus.C, 0)
                        .WithLocusMismatchCount(Locus.Dqb1, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (testInput.DonorType == DonorType.Cord)
            {
                searchRequestBuilder = searchRequestBuilder.WithSearchType(DonorType.Cord);
            }

            return searchRequestBuilder.Build();
        }

        private static void WriteResultsToCsv(IEnumerable<TestOutput> results, string outputFilePath)
        {
            var baseDirectory = outputFilePath ?? DefaultOutputDirectory;
            var outputFileName = $"{baseDirectory}/PerformanceTestResults{DateTime.UtcNow:yyyyMMddHHmm}.csv";
            using (var fileStream = new FileStream(outputFileName, FileMode.Append, FileAccess.Write))
            using (TextWriter writer = new StreamWriter(fileStream))
            {
                var csv = new CsvWriter(writer);
                csv.WriteRecords(results);
            }
        }
    }
}