using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Test.Performance.Models;

namespace Atlas.MatchingAlgorithm.Test.Performance
{
    public static class TestCases
    {
        /// <summary>
        /// Default instance = local
        /// Add additional object and pass in to TestInput class to test a new environment
        /// Only local environment info should be checked in.
        /// </summary>
        public static readonly AlgorithmInstanceInfo LocalAlgorithmInstanceInfo = new AlgorithmInstanceInfo
        {
            BaseUrl = "http://localhost:30508",
            Apikey = "test-key",
            Environment = Environment.Local,
        };

        /// <summary>
        /// This can be used to add some test case specific notes to a set of results
        /// i.e. when testing various tweaks to the algorithm, this can help keep track of what changed
        /// </summary>
        public static string Notes = "";

        private static readonly PatientInfo UatPatient489252 = new PatientInfo
        {
            PatientId = "489252",
            Hla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*24:02", "*29:02"),
                valueB: new LocusInfo<string>("*45:01", "*15:01"),
                valueC: new LocusInfo<string>("*03:03", "*06:02"),
                valueDrb1: new LocusInfo<string>("*04:01", "*11:01"),
                valueDqb1: new LocusInfo<string>("*03:01", "*03:02")),
        };

        private static readonly PatientInfo LivePatient495317 = new PatientInfo
        {
            PatientId = "495317 (live)",
            Hla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*03:01:01:01", "*66:01:01:01"),
                valueB: new LocusInfo<string>("*07:02:01:01", "*35:01:01"),
                valueC: new LocusInfo<string>("*04:01:01", "*07:02:01:03"),
                valueDrb1: new LocusInfo<string>("*11:01:01", "*10:01:01"),
                valueDqb1: new LocusInfo<string>("*05:01", "*05:02:01")
            ),
        };

        private static readonly PatientInfo LivePatient496738 = new PatientInfo
        {
            PatientId = "496738 (live)",
            Hla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*01:01", "*02:01"),
                valueB: new LocusInfo<string>("*07:02", "*08:01"),
                valueC: new LocusInfo<string>("*07:01", "*07:02"),
                valueDrb1: new LocusInfo<string>("*15:01", "*03:01"),
                valueDqb1: new LocusInfo<string>("*06:02", "*02:01")
            ),
        };

        private static readonly PatientInfo LivePatient496272 = new PatientInfo
        {
            PatientId = "496272 (live)",
            Hla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*03:01", "*66:01"),
                valueB: new LocusInfo<string>("*07:02", "*07:06"),
                valueC: new LocusInfo<string>("*07:02", "*15:05"),
                valueDrb1: new LocusInfo<string>("*15:01", "*13:01"),
                valueDqb1: new LocusInfo<string>("*06:02", "*06:03")
            ),
        };

        private static readonly PatientInfo LivePatient496345 = new PatientInfo
        {
            PatientId = "496345 (live)",
            Hla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>("*24:02", "*29:02"),
                valueB: new LocusInfo<string>("*45:01", "*15:01"),
                valueC: new LocusInfo<string>("*03:03", "*06:02"),
                valueDrb1: new LocusInfo<string>("*04:01", "*11:01"),
                valueDqb1: new LocusInfo<string>("*03:01", "*03:02")
            ),
        };

        public static readonly IEnumerable<TestInput> TestInputs = new List<TestInput>
        {
            new TestInput(UatPatient489252)
            {
                SearchType = SearchType.Drb1MismatchThreeLocus,
            },

            new TestInput(LivePatient495317)
            {
                SearchType = SearchType.SixOutOfSix,
                SolarSearchElapsedMilliseconds = 0,
                SolarSearchMatchedDonors = 0,
            },

            new TestInput(LivePatient495317)
            {
                SearchType = SearchType.TenOutOfTen,
                SolarSearchElapsedMilliseconds = 0,
                SolarSearchMatchedDonors = 0,
            },

            new TestInput(LivePatient495317)
            {
                SearchType = SearchType.AMismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 5000,
                SolarSearchMatchedDonors = 30,
            },

            new TestInput(LivePatient495317)
            {
                SearchType = SearchType.BMismatchThreeLocus,
            },

            new TestInput(LivePatient495317)
            {
                SearchType = SearchType.Drb1MismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 6000,
                SolarSearchMatchedDonors = 95,
            },

            new TestInput(LivePatient496272)
            {
                SearchType = SearchType.AMismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 10550,
                SolarSearchMatchedDonors = 656,
            },

            new TestInput(LivePatient496345)
            {
                SearchType = SearchType.SixOutOfSix,
                SolarSearchElapsedMilliseconds = 13000,
                SolarSearchMatchedDonors = 3,
            },

            new TestInput(LivePatient496345)
            {
                SearchType = SearchType.AMismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 46000,
                SolarSearchMatchedDonors = 845,
            },

            new TestInput(LivePatient496345)
            {
                SearchType = SearchType.BMismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 16000,
                SolarSearchMatchedDonors = 657,
            },

            new TestInput(LivePatient496345)
            {
                SearchType = SearchType.Drb1MismatchThreeLocus,
                SolarSearchElapsedMilliseconds = 15000,
                SolarSearchMatchedDonors = 797,
            },

            new TestInput(LivePatient496345)
            {
                SearchType = SearchType.TenOutOfTen,
            },

            // SLOW!
//            new TestInput (LivePatient496738)
//            {
//                SearchDonorType = SearchDonorType.SixOutOfSix,
//                SolarSearchElapsedMilliseconds = 65300,
//                SolarSearchMatchedDonors = 4980,
//            },
        };
    }
}