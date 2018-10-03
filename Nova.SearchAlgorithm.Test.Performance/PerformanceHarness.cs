using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.SearchAlgorithm.Test.Performance.Models;

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
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .ForRegistries(new []{RegistryCode.AN, RegistryCode.DKMS, RegistryCode.NHSBT, RegistryCode.WBS, RegistryCode.FRANCE, RegistryCode.NMDP})
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithSearchHla(new PhenotypeInfo<string>
                {
                    A_1 = "24:02",
                    A_2 = "29:02",
                    B_1 = "45:01",
                    B_2 = "15:01",
                    C_1 = "03:03",
                    C_2 = "06:02",
                    Drb1_1 = "04:01",
                    Drb1_2 = "11:01",
                    Dqb1_1 = "03:01",
                    Dqb1_2 = "03:02"
                })
                .Build();

            var metrics = await SearchTimingService.TimeSearchRequest(searchRequest, AlgorithmInstanceInfo);
        }
    }
}