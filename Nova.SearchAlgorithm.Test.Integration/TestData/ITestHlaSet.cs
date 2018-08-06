using Nova.SearchAlgorithm.Common.Models;
// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.TestData
{
    /// <summary>
    /// Phenotypes listed within a set must be fully matched to one another.
    /// </summary>
    public interface ITestHlaSet
    {
        /// <summary>
        /// A, B, DRB1 phenotype consisting of single allele names as found in v3.31.0 of hla_nom.
        /// Every locus is heterozygous.
        /// </summary>
        PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles { get; }

        /// <summary>
        /// A, B, C, DQB1 & DRB1 phenotype consisting of single allele names as found in v3.31.0 of hla_nom.
        /// Every locus is heterozygous.
        /// </summary>
        PhenotypeInfo<string> FiveLocus_SingleExpressingAlleles { get; }

        /// <summary>
        /// A, B, DRB1 phenotype consisting of XX Codes that include alleles listed in the
        /// 3 locus, single expressing alleles phenotype.
        /// Every locus is heterozygous.
        /// </summary>
        PhenotypeInfo<string> ThreeLocus_XxCodes { get; }

        /// <summary>
        /// A, B, C, DQB1 & DRB1 phenotype consisting of XX Codes that include alleles listed in the
        /// 5 locus, single expressing alleles phenotype.
        /// Every locus is heterozygous.
        /// </summary>
        PhenotypeInfo<string> FiveLocus_XxCodes { get; }
    }
}
