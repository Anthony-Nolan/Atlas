using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Performance.Models;

namespace Nova.SearchAlgorithm.Test.Performance
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var results = new List<TestOutput>();
            var testsRun = 0;
            
            foreach (var testInput in TestCases.TestInputs)
            {
                var testOutput = await RunSearch(testInput);
                results.Add(testOutput);
                testsRun++;
                Console.WriteLine($"Run {testsRun}/{TestCases.TestInputs.Count()} tests");
            }

            WriteResultsToCsv(results);
        }

        private static async Task<TestOutput> RunSearch(TestInput testInput)
        {
            var searchRequest = BuildSearchRequest(testInput);
            var metrics = await SearchTimingService.TimeSearchRequest(searchRequest, testInput.AlgorithmInstanceInfo);
            return new TestOutput(testInput, metrics);
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
                case SearchType.ThreeLocusMismatchAtA:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(1)
                        .WithLocusMismatchCount(Locus.A, 1);
                    break;
                case SearchType.ThreeLocusMismatchAtB:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(1)
                        .WithLocusMismatchCount(Locus.B, 1);
                    break;
                case SearchType.ThreeLocusMismatchAtDrb1:
                    searchRequestBuilder = searchRequestBuilder
                        .WithTotalMismatchCount(1)
                        .WithLocusMismatchCount(Locus.Drb1, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (testInput.IsAlignedRegistriesSearch)
            {
                searchRequestBuilder = searchRequestBuilder
                    .ForAdditionalRegistry(RegistryCode.DKMS)
                    .ForAdditionalRegistry(RegistryCode.FRANCE)
                    .ForAdditionalRegistry(RegistryCode.NHSBT)
                    .ForAdditionalRegistry(RegistryCode.NMDP)
                    .ForAdditionalRegistry(RegistryCode.WBS);
            }

            if (testInput.DonorType == DonorType.Cord)
            {
                searchRequestBuilder = searchRequestBuilder.WithSearchType(DonorType.Cord);
            }

            return searchRequestBuilder.Build();
        }

        private static void WriteResultsToCsv(IEnumerable<TestOutput> results)
        {
            var baseDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)?.Replace("\\bin\\Debug", "");
            var outputFileName = $"{baseDirectory}/PerformanceTestResults{DateTime.UtcNow:yyyyMMddhhmm}.csv";
            using (var fileStream = new FileStream(outputFileName, FileMode.Append, FileAccess.Write))
            using (TextWriter writer = new StreamWriter(fileStream))
            {
                var csv = new CsvWriter(writer);
                csv.WriteRecords(results);
            }
        }
    }
}