using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData
{
    /// <summary>
    /// Holds sets of HLA phenotypes that can be re-used across the integration test suite.
    /// Phenotypes in Set1 are mismatched at every position to those in Set2.
    /// </summary>
    public class SampleTestHlas
    {
        public class HeterozygousSet1 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "01:01:02",
                    Position2 = "02:01:01:02L",
                },
                B =
                {
                    Position1 = "07:68:01",
                    Position2 = "08:01:01:01",
                },
                Drb1 =
                {
                    Position1 = "01:01:01",
                    Position2 = "03:02:01"
                }
            };

            public PhenotypeInfo<string> SixLocus_SingleExpressingAlleles => new HlaNamePhenotypeBuilder(ThreeLocus_SingleExpressingAlleles)
                .WithHlaNameAt(Locus.C, LocusPosition.One, "01:02:01:01")
                .WithHlaNameAt(Locus.C, LocusPosition.Two, "02:02:01")
                .WithHlaNameAt(Locus.Dpb1, LocusPosition.One, "01:01:01:01")
                .WithHlaNameAt(Locus.Dpb1, LocusPosition.Two, "09:01:01")
                .WithHlaNameAt(Locus.Dqb1, LocusPosition.One, "02:01:11")
                .WithHlaNameAt(Locus.Dqb1, LocusPosition.Two, "03:01:01:01")
                .Build();

            public PhenotypeInfo<string> SixLocus_ExpressingAlleles_WithTruncatedNames => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "01:01",
                    Position2 = "02:01",
                },
                B =
                {
                    Position1 = "07:68",
                    Position2 = "08:01",
                },
                C =
                {
                    Position1 = "01:02",
                    Position2 = "02:02",
                },
                Dpb1 =
                {
                    Position1 = "01:01",
                    Position2 = "09:01",
                },
                Dqb1 =
                {
                    Position1 = "02:01",
                    Position2 = "03:01",
                },
                Drb1 =
                {
                    Position1 = "01:01",
                    Position2 = "03:02"
                }
            };

            public PhenotypeInfo<string> SixLocus_XxCodes => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "01:XX",
                    Position2 = "02:XX",
                },
                B =
                {
                    Position1 = "07:XX",
                    Position2 = "08:XX",
                },
                C =
                {
                    Position1 = "01:XX",
                    Position2 = "02:XX",
                },
                Dpb1 =
                {
                    Position1 = "01:XX",
                    Position2 = "09:XX",
                },
                Dqb1 =
                {
                    Position1 = "02:XX",
                    Position2 = "03:XX",
                },
                Drb1 =
                {
                    Position1 = "01:XX",
                    Position2 = "03:XX"
                }
            };

            public PhenotypeInfo<string> FiveLocus_Serologies => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "1",
                    Position2 = "2",
                },
                B =
                {
                    Position1 = "7",
                    Position2 = "8",
                },
                C =
                {
                    Position1 = "1",
                    Position2 = "2",
                },
                Dqb1 =
                {
                    Position1 = "2",
                    Position2 = "3",
                },
                Drb1 =
                {
                    Position1 = "1",
                    Position2 = "3"
                }
            };
        }

        public class HeterozygousSet2 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "03:02:03",
                    Position2 = "11:01:01:01",
                },
                B =
                {
                    Position1 = "13:01:02",
                    Position2 = "14:06:01",
                },
                Drb1 =
                {
                    Position1 = "04:05:01:01",
                    Position2 = "08:02:04"
                }
            };

            public PhenotypeInfo<string> SixLocus_SingleExpressingAlleles => new HlaNamePhenotypeBuilder(ThreeLocus_SingleExpressingAlleles)
                .WithHlaNameAt(Locus.C, LocusPosition.One, "03:02:01")
                .WithHlaNameAt(Locus.C, LocusPosition.Two, "04:42:01")
                .WithHlaNameAt(Locus.Dpb1, LocusPosition.One, "39:01:01:04")
                .WithHlaNameAt(Locus.Dpb1, LocusPosition.Two, "124:01:01:01")
                .WithHlaNameAt(Locus.Dqb1, LocusPosition.One, "04:02:10")
                .WithHlaNameAt(Locus.Dqb1, LocusPosition.Two, "05:01:01:05")
                .Build();

            public PhenotypeInfo<string> SixLocus_ExpressingAlleles_WithTruncatedNames => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "03:02",
                    Position2 = "11:01",
                },
                B =
                {
                    Position1 = "13:01",
                    Position2 = "14:06",
                },
                C =
                {
                    Position1 = "03:02",
                    Position2 = "04:42",
                },
                Dpb1 =
                {
                    Position1 = "39:01",
                    Position2 = "124:01",
                },
                Dqb1 =
                {
                    Position1 = "04:02",
                    Position2 = "05:01",
                },
                Drb1 =
                {
                    Position1 = "04:05",
                    Position2 = "08:02"
                }
            };

            public PhenotypeInfo<string> SixLocus_XxCodes => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "03:XX",
                    Position2 = "11:XX",
                },
                B =
                {
                    Position1 = "13:XX",
                    Position2 = "14:XX",
                },
                C =
                {
                    Position1 = "03:XX",
                    Position2 = "04:XX",
                },
                Dpb1 =
                {
                    Position1 = "39:XX",
                    Position2  = "124:XX",
                },
                Dqb1 =
                {
                    Position1 = "04:XX",
                    Position2 = "05:XX",
                },
                Drb1 =
                {
                    Position1 = "04:XX",
                    Position2 = "08:XX"
                }
            };

            public PhenotypeInfo<string> FiveLocus_Serologies => new PhenotypeInfo<string>
            {
                A =
                {
                    Position1 = "3",
                    Position2 = "11",
                },
                B =
                {
                    Position1 = "13",
                    Position2 = "14",
                },
                C =
                {
                    Position1 = "3",
                    Position2 = "4",
                },
                Dqb1 =
                {
                    Position1 = "4",
                    Position2 = "5",
                },
                Drb1 =
                {
                    Position1 = "4",
                    Position2 = "8"
                }
            };
        }
    }
}