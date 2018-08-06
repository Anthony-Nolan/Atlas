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
                A_1 = "01:02",
                A_2 = "02:01:01:02L",
                B_1 = "07:68:01",
                B_2 = "08:02",
                DRB1_1 = "01:01:01",
                DRB1_2 = "03:07"
            };

            public PhenotypeInfo<string> FiveLocus_SingleExpressingAlleles => new HlaNamePhenotypeBuilder(ThreeLocus_SingleExpressingAlleles)
                    .WithHlaNameAt(Locus.C, TypePositions.One, "01:02:01:01")
                    .WithHlaNameAt(Locus.C, TypePositions.Two, "02:02:01")
                    .WithHlaNameAt(Locus.Dqb1, TypePositions.One, "02:15")
                    .WithHlaNameAt(Locus.Dqb1, TypePositions.Two, "03:01:01:01")
                    .Build();

            public PhenotypeInfo<string> ThreeLocus_XxCodes => new PhenotypeInfo<string>
            {
                A_1 = "01:XX",
                A_2 = "02:XX",
                B_1 = "07:XX",
                B_2 = "08:XX",
                DRB1_1 = "01:XX",
                DRB1_2 = "03:XX"
            };

            public PhenotypeInfo<string> FiveLocus_XxCodes => new HlaNamePhenotypeBuilder(ThreeLocus_XxCodes)
                .WithHlaNameAt(Locus.C, TypePositions.One, "01:XX")
                .WithHlaNameAt(Locus.C, TypePositions.Two, "02:XX")
                .WithHlaNameAt(Locus.Dqb1, TypePositions.One, "02:XX")
                .WithHlaNameAt(Locus.Dqb1, TypePositions.Two, "03:XX")
                .Build();
        }

        public class HeterozygousSet2 : ITestHlaSet
        {
            public PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles => new PhenotypeInfo<string>
            {
                A_1 = "03:45",
                A_2 = "11:01:01:01",
                B_1 = "13:01:02",
                B_2 = "14:47",
                DRB1_1 = "04:05:01:01",
                DRB1_2 = "08:09"
            };

            public PhenotypeInfo<string> FiveLocus_SingleExpressingAlleles => new HlaNamePhenotypeBuilder(ThreeLocus_SingleExpressingAlleles)
                .WithHlaNameAt(Locus.C, TypePositions.One, "03:02:01")
                .WithHlaNameAt(Locus.C, TypePositions.Two, "04:38")
                .WithHlaNameAt(Locus.Dqb1, TypePositions.One, "04:04")
                .WithHlaNameAt(Locus.Dqb1, TypePositions.Two, "05:01:01:05")
                .Build();

            public PhenotypeInfo<string> ThreeLocus_XxCodes => new PhenotypeInfo<string>
            {
                A_1 = "03:XX",
                A_2 = "11:XX",
                B_1 = "13:XX",
                B_2 = "14:XX",
                DRB1_1 = "04:XX",
                DRB1_2 = "08:XX"
            };

            public PhenotypeInfo<string> FiveLocus_XxCodes => new HlaNamePhenotypeBuilder(ThreeLocus_XxCodes)
                .WithHlaNameAt(Locus.C, TypePositions.One, "03:XX")
                .WithHlaNameAt(Locus.C, TypePositions.Two, "04:XX")
                .WithHlaNameAt(Locus.Dqb1, TypePositions.One, "04:XX")
                .WithHlaNameAt(Locus.Dqb1, TypePositions.Two, "05:XX")
                .Build();
        }
    }
}
