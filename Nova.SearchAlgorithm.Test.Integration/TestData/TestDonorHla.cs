using Nova.SearchAlgorithm.Common.Models;
// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.TestData
{
    /// <summary>
    /// Phenotypes listed within a set must be fully matched to one another.
    /// </summary>
    public interface ITestDonorHlaSet
    {
        /// <summary>
        /// A, B, DRB1 phenotype consisting of single allele names as found in v3.31.0 of hla_nom.
        /// Every locus is heterozygous.
        /// </summary>
        PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles { get; }

        /// <summary>
        /// A, B, DRB1 phenotype consisting of XX Codes that include alleles listed in single
        /// expressing alleles phenotype.
        /// Every locus is heterozygous.
        /// </summary>
        PhenotypeInfo<string> ThreeLocus_XxCodes { get; }
    }

    /// <summary>
    /// Holds a set of donor HLA phenotypes that can be re-used across the integration test suite.
    /// Phenotypes in Set1 are mismatched at every position to those in Set2.
    /// </summary>
    public class TestDonorHla
    {
        public class HeterozygousSet1 : ITestDonorHlaSet
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

            public PhenotypeInfo<string> ThreeLocus_XxCodes => new PhenotypeInfo<string>
            {
                A_1 = "01:XX",
                A_2 = "02:XX",
                B_1 = "07:XX",
                B_2 = "08:XX",
                DRB1_1 = "01:XX",
                DRB1_2 = "03:XX"
            };
        }

        public class HeterozygousSet2 : ITestDonorHlaSet
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

            public PhenotypeInfo<string> ThreeLocus_XxCodes => new PhenotypeInfo<string>
            {
                A_1 = "03:XX",
                A_2 = "11:XX",
                B_1 = "13:XX",
                B_2 = "14:XX",
                DRB1_1 = "04:XX",
                DRB1_2 = "08:XX"
            };
        }
    }
}
