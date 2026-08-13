using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation.SerologyToSerology
{
    public class SerologyToSerologyTestCaseSources
    {
        public static readonly object[] ExpectedSerologyInfos =
        {
            new object[]
            {
                // Broad serology has at least one Split with an Associated antigen
                "A", Locus.A, "28", SerologySubtype.Broad,
                new[]
                {
                    new object[]{"28", SerologySubtype.Broad, true},
                    new object[]{"68", SerologySubtype.Split, false},
                    new object[]{"69", SerologySubtype.Split, false},
                    new object[]{"6801", SerologySubtype.Associated, false },
                    new object[]{"6810", SerologySubtype.Associated, false },
                    new object[]{"6813", SerologySubtype.Associated, false },
                    new object[]{"6836", SerologySubtype.Associated, false }
                }
            },
            new object[]
            {
                //Broad serology has Splits with no Associated antigens
                "DQ", Locus.Dqb1, "1", SerologySubtype.Broad,
                new[]
                {
                    new object[]{"1", SerologySubtype.Broad, true},
                    new object[]{"5",SerologySubtype.Split, false},
                    new object[]{"6",SerologySubtype.Split, false}
                }
            },
            new object[]
            {
                // Broad serology has its own Associated antigen
                "B", Locus.B, "21", SerologySubtype.Broad,
                new[]
                {
                    new object[]{"21", SerologySubtype.Broad, true},
                    new object[]{"4005", SerologySubtype.Associated, false},
                    new object[]{"49", SerologySubtype.Split, false},
                    new object[]{"50", SerologySubtype.Split, false}
                }
            },
            new object[]
            {
                // Split serology has at least one Associated antigen
                "B", Locus.B, "51", SerologySubtype.Split,
                new[]
                {
                    new object[]{"51",SerologySubtype.Split, true},
                    new object[]{"5",SerologySubtype.Broad, false},
                    new object[]{"5101",SerologySubtype.Associated, false},
                    new object[]{"5102", SerologySubtype.Associated, false},
                    new object[]{"5103",SerologySubtype.Associated, false},
                    new object[]{"5107", SerologySubtype.Associated, false},
                    new object[]{"5119",SerologySubtype.Associated, false}
                }
            },
            new object[]
            {
                // Split serology has no Associated antigens
                "DR", Locus.Drb1, "17", SerologySubtype.Split,
                new[]
                {
                    new object[]{"17",SerologySubtype.Split, true},
                    new object[]{"3",SerologySubtype.Broad, false}
                }
            },
            new object[]
            {
                // Associated serology is direct child of a Split antigen
                "B", Locus.B, "3902", SerologySubtype.Associated,
                new[]
                {
                    new object[] {"3902",SerologySubtype.Associated, true},
                    new object[] {"39", SerologySubtype.Split, false},
                    new object[] {"16",SerologySubtype.Broad, false}
                }
            },
            new object[]
            {
                // Associated serology is direct child of a Broad antigen
                "B", Locus.B, "4005", SerologySubtype.Associated,
                new[]
                {
                    new object[] {"4005", SerologySubtype.Associated, true},
                    new object[] {"21",SerologySubtype.Broad, false}
                }
            },
            new object[]
            {
                // Associated serology is direct child of a Not-Split antigen
                "DR", Locus.Drb1, "0103", SerologySubtype.Associated,
                new[]
                {
                    new object[] {"0103",SerologySubtype.Associated, true},
                    new object[] {"1",SerologySubtype.NotSplit, false}
                }
            },
            new object[]
            {
                // Not-Split serology has at least one Associated antigen
                "A", Locus.A, "3", SerologySubtype.NotSplit,
                new[]
                {
                    new object[]{"3",SerologySubtype.NotSplit, true},
                    new object[]{"0301",SerologySubtype.Associated, false},
                    new object[]{"0305",SerologySubtype.Associated, false},
                    new object[]{"0323",SerologySubtype.Associated, false}
                }
            },
            new object[]
            {
                // Not-Split serology with no Associated antigens
                "DR", Locus.Drb1, "9", SerologySubtype.NotSplit,
                new[]
                {
                    new object[] {"9",SerologySubtype.NotSplit, true}
                }
            },
        };
    }
}
