using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation.AlleleToSerology
{
    public class AlleleToSerologyTestCaseSources
    {
        public static readonly object[] ExpressingAllelesMatchingSerologies =
        {
            new object[]
            {
                // normal Allele
                Locus.A, "01:01:01:01",
                new[]
                {
                    new object[] {"A", "1", SerologySubtype.NotSplit, true}
                }
            },
            new object[]
            {
                // low Allele
                Locus.B, "39:01:01:02L",
                new[]
                {
                    new object[] {"B", "3901", SerologySubtype.Associated, true},
                    new object[] {"B", "39", SerologySubtype.Split, false},
                    new object[] {"B", "16", SerologySubtype.Broad, false}
                }
            },
            new object[]
            {
                // questionable Allele
                Locus.C, "07:01:01:14Q",
                new[]
                {
                    new object[] {"Cw", "7", SerologySubtype.NotSplit, true}
                }

            },
            new object[]
            {
                // secreted Allele
                Locus.B, "44:02:01:02S",
                new[]
                {
                    new object[] {"B", "44", SerologySubtype.Split, true},
                    new object[] {"B", "12", SerologySubtype.Broad, false}
                }
            }
        };

        public static readonly object[] DeletedAllelesMatchingSerologies =
        {
            new object[]
            {
                // deleted allele with identical hla
                Locus.A, "11:53",
                new[]
                {
                    new object[] { "A", "11", SerologySubtype.NotSplit, true }
                }
            },
            new object[]
            {
                // deleted allele is null, but identical is expressing
                Locus.A, "01:34N",
                new[]
                {
                    new object[] { "A", "1", SerologySubtype.NotSplit, true }
                }
            },
            new object[]
            {
                // deleted allele is expressing, but identical is null
                Locus.A, "03:260",
                new object[][]{}
            }
        };

        public static readonly object[] AllelesMappedToSpecificSubtypeMatchingSerologies =
        {
            new object[]
            {
                // Broad with no Associated
                Locus.A, "26:10",
                new[]
                {
                    new object[] {"A", "10", SerologySubtype.Broad, true },
                    new object[] {"A", "25", SerologySubtype.Split, false },
                    new object[] {"A", "26", SerologySubtype.Split, false },
                    new object[] {"A", "34", SerologySubtype.Split, false },
                    new object[] {"A", "66", SerologySubtype.Split, false }
                }
            },
            new object[]
            {
                // Broad With Associated
                Locus.B, "40:26",
                new[]
                {
                    new object[] {"B", "21", SerologySubtype.Broad, true },
                    new object[] {"B", "4005", SerologySubtype.Associated, false },
                    new object[] {"B", "49", SerologySubtype.Split, false },
                    new object[] {"B", "50", SerologySubtype.Split, false },
                    new object[] {"B", "40", SerologySubtype.Broad, true },
                    new object[] {"B", "60", SerologySubtype.Split, false },
                    new object[] {"B", "61", SerologySubtype.Split, false }
                }
            },
            new object[]
            {
                // Split with no Associated
                Locus.C, "03:02:01",
                new[]
                {
                    new object[] {"Cw", "10", SerologySubtype.Split, true },
                    new object[] {"Cw", "3", SerologySubtype.Broad, false }
                }
            },
            new object[]
            {
                // Split with Associated
                Locus.Drb1, "14:01:01",
                new[]
                {
                    new object[] {"DR", "14", SerologySubtype.Split, true },
                    new object[] {"DR", "6", SerologySubtype.Broad, false },
                    new object[] {"DR", "1403", SerologySubtype.Associated, false },
                    new object[] {"DR", "1404", SerologySubtype.Associated, false }
                }
            },
            new object[]
            {
                // Associated directly to Broad
                Locus.B, "40:05:01:01",
                new[]
                {
                    new object[] {"B", "40", SerologySubtype.Broad, true },
                    new object[] {"B", "60", SerologySubtype.Split, false },
                    new object[] {"B", "61", SerologySubtype.Split, false },
                    new object[] {"B", "4005", SerologySubtype.Associated, true },
                    new object[] {"B", "21", SerologySubtype.Broad, false }
                }
            },
            new object[]
            {
                // Associated directly to Split
                Locus.A, "24:03:01:01",
                new[]
                {
                    new object[] {"A", "2403", SerologySubtype.Associated, true },
                    new object[] {"A", "24", SerologySubtype.Split, false },
                    new object[] {"A", "9", SerologySubtype.Broad, false }
                }
            },
            new object[]
            {
                // Associated directly to Not-Split
                Locus.Drb1, "01:03:02",
                new[]
                {
                    new object[] {"DR", "103", SerologySubtype.Associated, true },
                    new object[] {"DR", "1", SerologySubtype.NotSplit, false }
                }
            },
            new object[]
            {
                // Not-Split has Associated
                Locus.B, "07:02:27",
                new[]
                {
                    new object[] {"B", "7", SerologySubtype.NotSplit, true },
                    new object[] {"B", "703", SerologySubtype.Associated, false }
                }
            },
            new object[]
            {
                // Not-Split has no Associated
                Locus.Dqb1, "02:02:01:01",
                new[]
                {
                    new object[] {"DQ", "2", SerologySubtype.NotSplit, true }
                }
            }
        };

        private static readonly AlleleTestCase B15BroadAlleleTestCase = new AlleleTestCase{ Locus = Locus.B, Name = "15:33" };
        private static readonly AlleleTestCase B15SplitAlleleTestCase = new AlleleTestCase{ Locus = Locus.B, Name = "15:01:01:01" };
        private static readonly AlleleTestCase B70BroadAlleleTestCase = new AlleleTestCase{ Locus = Locus.B, Name = "15:09:01" };
        private static readonly AlleleTestCase B70SplitAlleleTestCase = new AlleleTestCase{ Locus = Locus.B, Name = "15:03:01:01" };
        private static readonly AlleleTestCase B15And70BroadAlleleTestCase = new AlleleTestCase{ Locus = Locus.B, Name = "15:36" };

        public static readonly object[] B15AllelesMatchingSerologies =
        {
            new object[]
            {
                B15BroadAlleleTestCase,
                new[]
                {
                    new object[] {"B", "15", SerologySubtype.Broad, true },
                    new object[] {"B", "62", SerologySubtype.Split, false },
                    new object[] {"B", "63", SerologySubtype.Split, false },
                    new object[] {"B", "75", SerologySubtype.Split, false },
                    new object[] {"B", "76", SerologySubtype.Split, false },
                    new object[] {"B", "77", SerologySubtype.Split, false }
                }
            },
            new object[]
            {
                B15SplitAlleleTestCase,
                new[]
                {
                    new object[] {"B", "62", SerologySubtype.Split, true },
                    new object[] {"B", "15", SerologySubtype.Broad, false }
                }
            },
            new object[]
            {
                B70BroadAlleleTestCase,
                new[]
                {
                    new object[] {"B", "70", SerologySubtype.Broad, true },
                    new object[] {"B", "71", SerologySubtype.Split, false },
                    new object[] {"B", "72", SerologySubtype.Split, false }
                }
            },
            new object[]
            {
                B70SplitAlleleTestCase,
                new[]
                {
                    new object[] {"B", "72", SerologySubtype.Split, true },
                    new object[] {"B", "70", SerologySubtype.Broad, false }
                }
            },
            new object[]
            {
                B15And70BroadAlleleTestCase,
                new[]
                {
                    new object[] {"B", "15", SerologySubtype.Broad, true },
                    new object[] {"B", "62", SerologySubtype.Split, false },
                    new object[] {"B", "63", SerologySubtype.Split, false },
                    new object[] {"B", "75", SerologySubtype.Split, false },
                    new object[] {"B", "76", SerologySubtype.Split, false },
                    new object[] {"B", "77", SerologySubtype.Split, false },
                    new object[] {"B", "70", SerologySubtype.Broad, true },
                    new object[] {"B", "71", SerologySubtype.Split, false },
                    new object[] {"B", "72", SerologySubtype.Split, false }
                }
            }
        };

        public static readonly object[] AllelesOfUnknownSerology =
        {
            new object[]
            {
                // No assignments
                Locus.C, "12:02:02:01",
                new object[][]{}
            },
            new object[]
            {
                // No assignments
                Locus.Dpb1, "01:01:01:01",
                new object[][]{}
            },
            new object[]
            {
                // Only has expert assignment
                Locus.C, "15:07",
                new[]
                {
                    new object[] {"Cw", "3", SerologySubtype.Broad, true},
                    new object[] {"Cw", "9", SerologySubtype.Split, false},
                    new object[] {"Cw", "10", SerologySubtype.Split, false}
                }
            }
        };
    }
}