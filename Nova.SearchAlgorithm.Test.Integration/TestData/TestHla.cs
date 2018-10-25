using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;

namespace Nova.SearchAlgorithm.Test.Integration.TestData
{
    /// <summary>
    /// Holds sets of HLA phenotypes that can be re-used across the integration test suite.
    /// Phenotypes in Set1 are mismatched at every position to those in Set2.
    /// </summary>
    public class TestHla
    {
        public class HeterozygousSet1 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A_1 = "01:01:02",
                A_2 = "02:01:01:02L",
                B_1 = "07:68:01",
                B_2 = "08:01:01:01",
                DRB1_1 = "01:01:01",
                DRB1_2 = "03:02:01"
            };

            public PhenotypeInfo<string> FiveLocus_SingleExpressingAlleles => new HlaNamePhenotypeBuilder(ThreeLocus_SingleExpressingAlleles)
                    .WithHlaNameAt(Locus.C, TypePosition.One, "01:02:01:01")
                    .WithHlaNameAt(Locus.C, TypePosition.Two, "02:02:01")
                    .WithHlaNameAt(Locus.Dqb1, TypePosition.One, "02:01:11")
                    .WithHlaNameAt(Locus.Dqb1, TypePosition.Two, "03:01:01:01")
                    .Build();

            public PhenotypeInfo<string> FiveLocus_ExpressingAlleles_WithTruncatedNames => new PhenotypeInfo<string>
            {
                A_1 = "01:01",
                A_2 = "02:01",
                B_1 = "07:68",
                B_2 = "08:01",
                C_1 = "01:02",
                C_2 = "02:02",
                DQB1_1 = "02:01",
                DQB1_2 = "03:01",
                DRB1_1 = "01:01",
                DRB1_2 = "03:02"
            };

        public PhenotypeInfo<string> FiveLocus_XxCodes => new PhenotypeInfo<string>
            {
                A_1 = "01:XX",
                A_2 = "02:XX",
                B_1 = "07:XX",
                B_2 = "08:XX",
                C_1 = "01:XX",
                C_2 = "02:XX",
                DQB1_1 = "02:XX",
                DQB1_2 = "03:XX",
                DRB1_1 = "01:XX",
                DRB1_2 = "03:XX"
            };

            public PhenotypeInfo<string> FiveLocus_Serologies => new PhenotypeInfo<string>
            {
                A_1 = "1",
                A_2 = "2",
                B_1 = "7",
                B_2 = "8",
                C_1 = "1",
                C_2 = "2",
                DQB1_1 = "2",
                DQB1_2 = "3",
                DRB1_1 = "1",
                DRB1_2 = "3"
            };
        }

        public class HeterozygousSet2 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A_1 = "03:02:03",
                A_2 = "11:01:01:01",
                B_1 = "13:01:02",
                B_2 = "14:06:01",
                DRB1_1 = "04:05:01:01",
                DRB1_2 = "08:02:04"
            };

            public PhenotypeInfo<string> FiveLocus_SingleExpressingAlleles => new HlaNamePhenotypeBuilder(ThreeLocus_SingleExpressingAlleles)
                .WithHlaNameAt(Locus.C, TypePosition.One, "03:02:01")
                .WithHlaNameAt(Locus.C, TypePosition.Two, "04:42:01")
                .WithHlaNameAt(Locus.Dqb1, TypePosition.One, "04:02:10")
                .WithHlaNameAt(Locus.Dqb1, TypePosition.Two, "05:01:01:05")
                .Build();

            public PhenotypeInfo<string> FiveLocus_ExpressingAlleles_WithTruncatedNames => new PhenotypeInfo<string>
            {
                A_1 = "03:02",
                A_2 = "11:01",
                B_1 = "13:01",
                B_2 = "14:06",
                C_1 = "03:02",
                C_2 = "04:42",
                DQB1_1 = "04:02",
                DQB1_2 = "05:01",
                DRB1_1 = "04:05",
                DRB1_2 = "08:02"
            };

            public PhenotypeInfo<string> FiveLocus_XxCodes => new PhenotypeInfo<string>
            {
                A_1 = "03:XX",
                A_2 = "11:XX",
                B_1 = "13:XX",
                B_2 = "14:XX",
                C_1 = "03:XX",
                C_2 = "04:XX",
                DQB1_1 = "04:XX",
                DQB1_2 = "05:XX",
                DRB1_1 = "04:XX",
                DRB1_2 = "08:XX"
            };

            public PhenotypeInfo<string> FiveLocus_Serologies => new PhenotypeInfo<string>
            {
                A_1 = "3",
                A_2 = "11",
                B_1 = "13",
                B_2 = "14",
                C_1 = "3",
                C_2 = "4",
                DQB1_1 = "4",
                DQB1_2 = "5",
                DRB1_1 = "4",
                DRB1_2 = "8"
            };
        }
    }
}
