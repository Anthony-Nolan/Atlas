using System.Collections.Generic;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Performance.Models;

namespace Nova.SearchAlgorithm.Test.Performance
{
    public static class TestCases
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

        /// <summary>
        /// This can be used to add some test case specific notes to a set of results
        /// i.e. when testing various tweaks to the algorithm, this can help keep track of what changed
        /// </summary>
        public static string Notes = "NOVA-1961: Donors batched (passing tests)";

        private static readonly PatientInfo UatPatient489252 = new PatientInfo
        {
            PatientId = "489252",
            Hla = new PhenotypeInfo<string>
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
            },
        };
        
        private static readonly PatientInfo LivePatient495317 = new PatientInfo
        {
            PatientId = "495317 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A_1 = "*03:01:01:01",
                A_2 = "*66:01:01:01",
                B_1 = "*07:02:01:01",
                B_2 = "*35:01:01",
                C_1 = "*04:01:01",
                C_2 = "*07:02:01:03",
                Drb1_1 = "*11:01:01",
                Drb1_2 = "*10:01:01",
                Dqb1_1 = "*05:01",
                Dqb1_2 = "*05:02:01",
            },
        };
        
        private static readonly PatientInfo LivePatient496738 = new PatientInfo
        {
            PatientId = "496738 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A_1 = "*01:01",
                A_2 = "*02:01",
                B_1 = "*07:02",
                B_2 = "*08:01",
                C_1 = "*07:01",
                C_2 = "*07:02",
                Drb1_1 = "*15:01",
                Drb1_2 = "*03:01",
                Dqb1_1 = "*06:02",
                Dqb1_2 = "*02:01",
            },
        };
        
        private static readonly PatientInfo LivePatient496272 = new PatientInfo
        {
            PatientId = "496272 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A_1 = "*03:01",
                A_2 = "*66:01",
                B_1 = "*07:02",
                B_2 = "*07:06",
                C_1 = "*07:02",
                C_2 = "*15:05",
                Drb1_1 = "*15:01",
                Drb1_2 = "*13:01",
                Dqb1_1 = "*06:02",
                Dqb1_2 = "*06:03",
            },
        };

        public static readonly IEnumerable<TestInput> TestInputs = new List<TestInput>
        {
//            new TestInput
//            {
//                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
//                PatientId = UatPatient489252.PatientId,
//                Hla = UatPatient489252.Hla,
//                DonorType = DonorType.Adult,
//                IsAlignedRegistriesSearch = true,
//                SearchType = SearchType.SixOutOfSix,
//            },
//            
//            new TestInput
//            {
//                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
//                PatientId = UatPatient489252.PatientId,
//                Hla = UatPatient489252.Hla,
//                DonorType = DonorType.Adult,
//                IsAlignedRegistriesSearch = true,
//                SearchType = SearchType.AMismatchThreeLocus,
//                SolarSearchElapsedMilliseconds = 48000,
//                SolarSearchMatchedDonors = 845
//            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = UatPatient489252.PatientId,
                Hla = UatPatient489252.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.Drb1MismatchThreeLocus,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = LivePatient495317.PatientId,
                Hla = LivePatient495317.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.SixOutOfSix,
                SolarSearchElapsedMilliseconds = 0,
                SolarSearchMatchedDonors = 0,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = LivePatient495317.PatientId,
                Hla = LivePatient495317.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.TenOutOfTen,
                SolarSearchElapsedMilliseconds = 0,
                SolarSearchMatchedDonors = 0,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = LivePatient495317.PatientId,
                Hla = LivePatient495317.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.AMismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 5000,
                SolarSearchMatchedDonors = 30,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = LivePatient495317.PatientId,
                Hla = LivePatient495317.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.BMismatchThreeLocus,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = LivePatient495317.PatientId,
                Hla = LivePatient495317.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.Drb1MismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 6000,
                SolarSearchMatchedDonors = 95,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = LivePatient496272.PatientId,
                Hla = LivePatient496272.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.AMismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 10550,
                SolarSearchMatchedDonors = 656,
            },
            
//            new TestInput
//            {
//                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
//                PatientId = LivePatient496738.PatientId,
//                Hla = LivePatient496738.Hla,
//                DonorType = DonorType.Adult,
//                IsAlignedRegistriesSearch = true,
//                SearchType = SearchType.SixOutOfSix,
//                SolarSearchElapsedMilliseconds = 65300,
//                SolarSearchMatchedDonors = 4980,
//            },
        };
    }
}