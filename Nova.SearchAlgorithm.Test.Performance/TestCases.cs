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
            PatientId = "495317",
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

        public static readonly IEnumerable<TestInput> TestInputs = new List<TestInput>
        {
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = UatPatient489252.PatientId,
                Hla = UatPatient489252.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.SixOutOfSix,
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = UatPatient489252.PatientId,
                Hla = UatPatient489252.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.ThreeLocusMismatchAtA,
                SolarSearchElapsedMilliseconds = 48000,
                SolarSearchMatchedDonors = 845
            },
            
            new TestInput
            {
                AlgorithmInstanceInfo = AlgorithmInstanceInfo,
                PatientId = UatPatient489252.PatientId,
                Hla = UatPatient489252.Hla,
                DonorType = DonorType.Adult,
                IsAlignedRegistriesSearch = true,
                SearchType = SearchType.ThreeLocusMismatchAtDrb1,
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
                SearchType = SearchType.ThreeLocusMismatchAtA,
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
                SearchType = SearchType.ThreeLocusMismatchAtDrb1,
                SolarSearchElapsedMilliseconds = 6000,
                SolarSearchMatchedDonors = 95,
            },
        };
    }
}