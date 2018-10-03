using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using CsvHelper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Performance.Models;
using Environment = Nova.SearchAlgorithm.Test.Performance.Models.Environment;

namespace Nova.SearchAlgorithm.Test.Performance
{
    internal class Program
    {
        /// <summary>
        /// This should be manually updated before running performance tests.
        /// Only local environment info should be checked in.
        /// </summary>
        private static readonly AlgorithmInstanceInfo AlgorithmInstanceInfo = new AlgorithmInstanceInfo
        {
            BaseUrl = "http://localhost:30508",
            Apikey = "test-key",
            Environment = Environment.Local,
            AzureStorageEnvironment = Environment.Local,
        };
        
        public static async Task Main(string[] args)
        {
            var patientHla = new PhenotypeInfo<string>
            {
                A_1 = "*24:02",
                A_2 = "*29:02",
                B_1 = "*45:01",
                B_2 = "*15:01",
                C_1 = "*03:03",
                C_2 = "*06:02",
                Drb1_1 = "*04:01",
                Drb1_2 = "*11:01",
                Dqb1_1 = "*03:01",
                Dqb1_2 = "*03:02",
            };
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .ForRegistries(new []{RegistryCode.AN, RegistryCode.DKMS, RegistryCode.NHSBT, RegistryCode.WBS, RegistryCode.FRANCE, RegistryCode.NMDP})
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithSearchHla(patientHla)
                .Build();

            var metrics = await SearchTimingService.TimeSearchRequest(searchRequest, AlgorithmInstanceInfo);

            var results = new List<TestOutput>
            {
                new TestOutput
                {
                    DonorId = "489252",
                    ElapsedMilliseconds = metrics.ElapsedMilliseconds,
                    DonorType = DonorType.Adult,
                    IsAlignedRegistriesSearch = true,
                    SearchType = SearchType.SixOutOfSix,
                    MatchedDonors = metrics.DonorsReturned,
                    HlaA1 = patientHla.A_1,
                    HlaA2 = patientHla.A_2,
                    HlaB1 = patientHla.B_1,
                    HlaB2 = patientHla.B_2,
                    HlaC1 = patientHla.C_1,
                    HlaC2 = patientHla.C_2,
                    HlaDqb11 = patientHla.Dqb1_1,
                    HlaDqb12 = patientHla.Dqb1_2,
                    HlaDrb11 = patientHla.Drb1_1,
                    HlaDrb12 = patientHla.Drb1_2,

                }
            };

            WriteResultsToCsv(results);
        }

        private static void WriteResultsToCsv(IEnumerable<TestOutput> results)
        {
            var baseDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)?.Replace("\\bin\\Debug", "");
            using (TextWriter writer = new StreamWriter($"{baseDirectory}/PerformanceTestResults{DateTime.UtcNow:yyyyMMddhhmm}.csv", false,
                System.Text.Encoding.UTF8))
            {
                var csv = new CsvWriter(writer);
                csv.WriteRecords(results);
            }
        }
    }
}