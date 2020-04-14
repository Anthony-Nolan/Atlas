namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class AlleleNameHistoriesTestCaseSources
    {
        private const string SameFromCurrentToV3000Name = "01:01:01:01";
        private const string DiscoveredAfterV3000Name = "07:242";
        private const string BeforeRenameName = "01:03";
        private const string AfterRenameName = "01:03:01:01";
        private const string IdenticalToAnotherAlleleName = "04:02:01:02";

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
                    new[] {"3330", SameFromCurrentToV3000Name}, new[] {"3320", SameFromCurrentToV3000Name},
                    new[] {"3310", SameFromCurrentToV3000Name}, new[] {"3300", SameFromCurrentToV3000Name},
                    new[] {"3290", SameFromCurrentToV3000Name}, new[] {"3280", SameFromCurrentToV3000Name},
                    new[] {"3270", SameFromCurrentToV3000Name}, new[] {"3260", SameFromCurrentToV3000Name},
                    new[] {"3250", SameFromCurrentToV3000Name}, new[] {"3240", SameFromCurrentToV3000Name},
                    new[] {"3230", SameFromCurrentToV3000Name}, new[] {"3220", SameFromCurrentToV3000Name},
                    new[] {"3210", SameFromCurrentToV3000Name}, new[] {"3200", SameFromCurrentToV3000Name},
                    new[] {"3190", SameFromCurrentToV3000Name}, new[] {"3180", SameFromCurrentToV3000Name},
                    new[] {"3170", SameFromCurrentToV3000Name}, new[] {"3160", SameFromCurrentToV3000Name},
                    new[] {"3150", SameFromCurrentToV3000Name}, new[] {"3140", SameFromCurrentToV3000Name},
                    new[] {"3131", SameFromCurrentToV3000Name}, new[] {"3120", SameFromCurrentToV3000Name},
                    new[] {"3110", SameFromCurrentToV3000Name}, new[] {"3100", SameFromCurrentToV3000Name},
                    new[] {"3090", SameFromCurrentToV3000Name}, new[] {"3080", SameFromCurrentToV3000Name},
                    new[] {"3070", SameFromCurrentToV3000Name}, new[] {"3060", SameFromCurrentToV3000Name},
                    new[] {"3050", SameFromCurrentToV3000Name}, new[] {"3040", SameFromCurrentToV3000Name},
                    new[] {"3030", SameFromCurrentToV3000Name}, new[] {"3020", SameFromCurrentToV3000Name},
                    new[] {"3010", SameFromCurrentToV3000Name}, new[] {"3000", SameFromCurrentToV3000Name}
                }
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                new[]
                {
                    new[] {"3330", DiscoveredAfterV3000Name}, new[] {"3320", DiscoveredAfterV3000Name},
                    new[] {"3310", DiscoveredAfterV3000Name}, new[] {"3300", DiscoveredAfterV3000Name},
                    new[] {"3290", DiscoveredAfterV3000Name}, new[] {"3280", DiscoveredAfterV3000Name},
                    new[] {"3270", DiscoveredAfterV3000Name}, new[] {"3260", DiscoveredAfterV3000Name},
                    new[] {"3250", DiscoveredAfterV3000Name}, new[] {"3240", DiscoveredAfterV3000Name},
                    new[] {"3230", DiscoveredAfterV3000Name}, new[] {"3220", DiscoveredAfterV3000Name},
                    new[] {"3210", DiscoveredAfterV3000Name}, new[] {"3200", DiscoveredAfterV3000Name},
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
                    new[] {"3330", AfterRenameName}, new[] {"3320", AfterRenameName},
                    new[] {"3310", AfterRenameName}, new[] {"3300", AfterRenameName},
                    new[] {"3290", AfterRenameName}, new[] {"3280", AfterRenameName},
                    new[] {"3270", BeforeRenameName}, new[] {"3260", BeforeRenameName},
                    new[] {"3250", BeforeRenameName}, new[] {"3240", BeforeRenameName},
                    new[] {"3230", BeforeRenameName}, new[] {"3220", BeforeRenameName},
                    new[] {"3210", BeforeRenameName}, new[] {"3200", BeforeRenameName},
                    new[] {"3190", BeforeRenameName}, new[] {"3180", BeforeRenameName},
                    new[] {"3170", BeforeRenameName}, new[] {"3160", BeforeRenameName},
                    new[] {"3150", BeforeRenameName}, new[] {"3140", BeforeRenameName},
                    new[] {"3131", BeforeRenameName}, new[] {"3120", BeforeRenameName},
                    new[] {"3110", BeforeRenameName}, new[] {"3100", BeforeRenameName},
                    new[] {"3090", BeforeRenameName}, new[] {"3080", BeforeRenameName},
                    new[] {"3070", BeforeRenameName}, new[] {"3060", BeforeRenameName},
                    new[] {"3050", BeforeRenameName}, new[] {"3040", BeforeRenameName},
                    new[] {"3030", BeforeRenameName}, new[] {"3020", BeforeRenameName},
                    new[] {"3010", BeforeRenameName}, new[] {"3000", BeforeRenameName}
                }
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                new[]
                {
                    new[] {"3330", null}, new[] {"3320", null}, new[] {"3310", null},
                    new[] {"3300", null}, new[] {"3290", IdenticalToAnotherAlleleName},
                    new[] {"3280", IdenticalToAnotherAlleleName}, new[] {"3270", IdenticalToAnotherAlleleName},
                    new[] {"3260", null}, new[] {"3250", null}, new[] {"3240", null},
                    new[] {"3230", null}, new[] {"3220", null}, new[] {"3210", null},
                    new[] {"3200", null}, new[] {"3190", null}, new[] {"3180", null},
                    new[] {"3170", null}, new[] {"3160", null}, new[] {"3150", null},
                    new[] {"3140", null}, new[] {"3131", null}, new[] {"3120", null},
                    new[] {"3110", null}, new[] {"3100", null}, new[] {"3090", null},
                    new[] {"3080", null}, new[] {"3070", null}, new[] {"3060", null},
                    new[] {"3050", null}, new[] {"3040", null}, new[] {"3030", null},
                    new[] {"3020", null}, new[] {"3010", null}, new[] {"3000", null}
                }
            }
        };

        public static readonly object[] ExpectedCurrentAlleleNames =
        {
            new object[]
            {
                AlleleWithSameNameFromCurrentVersionToVersion3000,
                SameFromCurrentToV3000Name
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                DiscoveredAfterV3000Name
            },
            new object[]
            {
                AlleleRenamed,
                AfterRenameName
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
                new[] { SameFromCurrentToV3000Name}
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                new[] { DiscoveredAfterV3000Name }
            },
            new object[]
            {
                AlleleRenamed,
                new[] { AfterRenameName, BeforeRenameName }
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                new[] { IdenticalToAnotherAlleleName }
            }
        };

        public static readonly object[] ExpectedMostRecentAlleleNames =
        {
            new object[]
            {
                AlleleWithSameNameFromCurrentVersionToVersion3000,
                SameFromCurrentToV3000Name
            },
            new object[]
            {
                AlleleDiscoveredAfterVersion3000,
                DiscoveredAfterV3000Name
            },
            new object[]
            {
                AlleleRenamed,
                AfterRenameName
            },
            new object[]
            {
                AlleleShownToBeIdenticalToAnotherAllele,
                IdenticalToAnotherAlleleName
            }
        };
    }
}
