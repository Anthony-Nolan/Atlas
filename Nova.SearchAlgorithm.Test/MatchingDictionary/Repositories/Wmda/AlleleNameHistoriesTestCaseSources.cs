namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class AlleleNameHistoriesTestCaseSources
    {
        private static readonly string[] AlleleWithSameNameFromCurrentVersionToVersion3000 = { "A*", "HLA00001" };
        private static readonly string[] AlleleDiscoveredAfterVersion3000 = { "B*", "HLA13015" };
        private static readonly string[] AlleleRenamed = { "A*", "HLA00003" };
        private static readonly string[] AlleleShownToBeIdenticalToAnotherAllele = { "DQB1*", "HLA16167" };

        public static readonly object[] SequencesToTest =
        {
            AlleleWithSameNameFromCurrentVersionToVersion3000,
            AlleleDiscoveredAfterVersion3000,
            AlleleRenamed,
            AlleleShownToBeIdenticalToAnotherAllele
        };

        public static readonly object[] ExpectedVersionedAlleleNames =
        {
            new object[]
            {
                AlleleWithSameNameFromCurrentVersionToVersion3000,
                new[]
                {
                    new[] {"3310", "01:01:01:01"}, new[] {"3300", "01:01:01:01"}, new[] {"3290", "01:01:01:01"}, new[] {"3280", "01:01:01:01"},
                    new[] {"3270", "01:01:01:01"}, new[] {"3260", "01:01:01:01"}, new[] {"3250", "01:01:01:01"}, new[] {"3240", "01:01:01:01"},
                    new[] {"3230", "01:01:01:01"}, new[] {"3220", "01:01:01:01"}, new[] {"3210", "01:01:01:01"}, new[] {"3200", "01:01:01:01"},
                    new[] {"3190", "01:01:01:01"}, new[] {"3180", "01:01:01:01"}, new[] {"3170", "01:01:01:01"}, new[] {"3160", "01:01:01:01"},
                    new[] {"3150", "01:01:01:01"}, new[] {"3140", "01:01:01:01"}, new[] {"3131", "01:01:01:01"}, new[] {"3120", "01:01:01:01"},
                    new[] {"3110", "01:01:01:01"}, new[] {"3100", "01:01:01:01"}, new[] {"3090", "01:01:01:01"}, new[] {"3080", "01:01:01:01"},
                    new[] {"3070", "01:01:01:01"}, new[] {"3060", "01:01:01:01"}, new[] {"3050", "01:01:01:01"}, new[] {"3040", "01:01:01:01"},
                    new[] {"3030", "01:01:01:01"}, new[] {"3020", "01:01:01:01"}, new[] {"3010", "01:01:01:01"}, new[] {"3000", "01:01:01:01"}
                }
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                new[]
                {
                    new[] {"3310", "07:242"}, new[] {"3300", "07:242"}, new[] {"3290", "07:242"}, new[] {"3280", "07:242"},
                    new[] {"3270", "07:242"}, new[] {"3260", "07:242"}, new[] {"3250", "07:242"}, new[] {"3240", "07:242"},
                    new[] {"3230", "07:242"}, new[] {"3220", "07:242"}, new[] {"3210", "07:242"}, new[] {"3200", "07:242"},
                    new[] {"3190", null}, new[] {"3180", null}, new[] {"3170", null}, new[] {"3160", null},
                    new[] {"3150", null}, new[] {"3140", null}, new[] {"3131", null}, new[] {"3120", null},
                    new[] {"3110", null}, new[] {"3100", null}, new[] {"3090", null}, new[] {"3080", null},
                    new[] {"3070", null}, new[] {"3060", null}, new[] {"3050", null}, new[] {"3040", null},
                    new[] {"3030", null}, new[] {"3020", null}, new[] {"3010", null}, new[] {"3000", null}
                }
            },
            new object[]
            {
                AlleleRenamed,
                new[]
                {
                    new[] {"3310", "01:03:01:01"}, new[] {"3300", "01:03:01:01"}, new[] {"3290", "01:03:01:01"}, new[] {"3280", "01:03:01:01"},
                    new[] {"3270", "01:03"}, new[] {"3260", "01:03"}, new[] {"3250", "01:03"}, new[] {"3240", "01:03"},
                    new[] {"3230", "01:03"}, new[] {"3220", "01:03"}, new[] {"3210", "01:03"}, new[] {"3200", "01:03"},
                    new[] {"3190", "01:03"}, new[] {"3180", "01:03"}, new[] {"3170", "01:03"}, new[] {"3160", "01:03"},
                    new[] {"3150", "01:03"}, new[] {"3140", "01:03"}, new[] {"3131", "01:03"}, new[] {"3120", "01:03"},
                    new[] {"3110", "01:03"}, new[] {"3100", "01:03"}, new[] {"3090", "01:03"}, new[] {"3080", "01:03"},
                    new[] {"3070", "01:03"}, new[] {"3060", "01:03"}, new[] {"3050", "01:03"}, new[] {"3040", "01:03"},
                    new[] {"3030", "01:03"}, new[] {"3020", "01:03"}, new[] {"3010", "01:03"}, new[] {"3000", "01:03"}
                }
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                new[]
                {
                    new[] {"3310", null}, new[] {"3300", null}, new[] {"3290", "04:02:01:02"}, new[] {"3280", "04:02:01:02"},
                    new[] {"3270", "04:02:01:02"}, new[] {"3260", null}, new[] {"3250", null}, new[] {"3240", null},
                    new[] {"3230", null}, new[] {"3220", null}, new[] {"3210", null}, new[] {"3200", null},
                    new[] {"3190", null}, new[] {"3180", null}, new[] {"3170", null}, new[] {"3160", null},
                    new[] {"3150", null}, new[] {"3140", null}, new[] {"3131", null}, new[] {"3120", null},
                    new[] {"3110", null}, new[] {"3100", null}, new[] {"3090", null}, new[] {"3080", null},
                    new[] {"3070", null}, new[] {"3060", null}, new[] {"3050", null}, new[] {"3040", null},
                    new[] {"3030", null}, new[] {"3020", null}, new[] {"3010", null}, new[] {"3000", null}
                }
            }
        };

        public static readonly object[] ExpectedCurrentAlleleNames =
        {
            new object[]
            {
                AlleleWithSameNameFromCurrentVersionToVersion3000,
                "01:01:01:01"
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                "07:242"
            },
            new object[]
            {
                AlleleRenamed,
                "01:03:01:01"
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                null
            }
        };

        public static readonly object[] ExpectedDistinctAlleleNames =
        {
            new object[]
            {
                AlleleWithSameNameFromCurrentVersionToVersion3000,
                new[] { "01:01:01:01"}
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                new[] { "07:242" }
            },
            new object[]
            {
                AlleleRenamed,
                new[] { "01:03:01:01", "01:03" }
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                new[] { "04:02:01:02" }
            }
        };

        public static readonly object[] ExpectedMostRecentAlleleNames =
        {
            new object[]
            {
                AlleleWithSameNameFromCurrentVersionToVersion3000,
                "01:01:01:01"
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                "07:242"
            },
            new object[]
            {
                AlleleRenamed,
                "01:03:01:01"
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                "04:02:01:02"
            }
        };
    }
}
