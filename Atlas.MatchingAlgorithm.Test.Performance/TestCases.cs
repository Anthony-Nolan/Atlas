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
            {
                A =
                {
                    Position1 = "*24:02",
                    Position2 = "*29:02",
                },
                B =
                {
                    Position1 = "*45:01",
                    Position2 = "*15:01",
                },
                C =
                {
                    Position1 = "*03:03",
                    Position2 = "*06:02",
                },
                Drb1 =
                {
                    Position1 = "*04:01",
                    Position2 = "*11:01",
                },
                Dqb1 =
                {
                    Position1 = "*03:01",
                    Position2 = "*03:02",
                }
            },
        };

        private static readonly PatientInfo LivePatient495317 = new PatientInfo
        {
            PatientId = "495317 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "*03:01:01:01",
                    Position2 = "*66:01:01:01",
                },
                B =
                {
                    Position1 = "*07:02:01:01",
                    Position2 = "*35:01:01",
                },
                C =
                {
                    Position1 = "*04:01:01",
                    Position2 = "*07:02:01:03",
                },
                Drb1 =
                {
                    Position1 = "*11:01:01",
                    Position2 = "*10:01:01",
                },
                Dqb1 =
                {
                    Position1 = "*05:01",
                    Position2 = "*05:02:01",
                }
            },
        };

        private static readonly PatientInfo LivePatient496738 = new PatientInfo
        {
            PatientId = "496738 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "*01:01",
                    Position2 = "*02:01",
                },
                B =
                {
                    Position1 = "*07:02",
                    Position2 = "*08:01",
                },
                C =
                {
                    Position1 = "*07:01",
                    Position2 = "*07:02",
                },
                Drb1 =
                {
                    Position1 = "*15:01",
                    Position2 = "*03:01",
                },
                Dqb1 =
                {
                    Position1 = "*06:02",
                    Position2 = "*02:01",
                }
            },
        };

        private static readonly PatientInfo LivePatient496272 = new PatientInfo
        {
            PatientId = "496272 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "*03:01",
                    Position2 = "*66:01",
                },
                B =
                {
                    Position1 = "*07:02",
                    Position2 = "*07:06",
                },
                C =
                {
                    Position1 = "*07:02",
                    Position2 = "*15:05",
                },
                Drb1 =
                {
                    Position1 = "*15:01",
                    Position2 = "*13:01",
                },
                Dqb1 =
                {
                    Position1 = "*06:02",
                    Position2 = "*06:03",
                }
            },
        };

        private static readonly PatientInfo LivePatient496345 = new PatientInfo
        {
            PatientId = "496345 (live)",
            Hla = new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "*24:02",
                    Position2 = "*29:02",
                },
                B =
                {
                    Position1 = "*45:01",
                    Position2 = "*15:01",
                },
                C =
                {
                    Position1 = "*03:03",
                    Position2 = "*06:02",
                },
                Drb1 =
                {
                    Position1 = "*04:01",
                    Position2 = "*11:01",
                },
                Dqb1 =
                {
                    Position1 = "*03:01",
                    Position2 = "*03:02",
                }
            },
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
//                SearchType = SearchType.SixOutOfSix,
//                SolarSearchElapsedMilliseconds = 65300,
//                SolarSearchMatchedDonors = 4980,
//            },
        };
    }
}