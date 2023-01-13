using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData
{
    /// <summary>
    /// Phenotypes listed within a set must be fully matched to one another.
    /// </summary>
    public interface ITestHlaSet
    {
        /// <summary>
        /// A, B, DRB1 phenotype consisting of single allele names as found in hla_nom.
        /// Allele names should be 3 or 4 fields long so they can be truncated to two fields in other test phenotypes.
        /// </summary>
        PhenotypeInfo<string> ThreeLocus_SingleExpressingAlleles { get; }

        /// <summary>
        /// A, B, C, DPB1, DQB1 & DRB1 phenotype consisting of single allele names as found in hla_nom.
        /// Allele names should be 3 or 4 fields long so they can be truncated to two fields in other test phenotypes.
        /// </summary>
        PhenotypeInfo<string> SixLocus_SingleExpressingAlleles { get; }

        /// <summary>
        /// A, B, C, DPB1, DQB1 & DRB1 phenotype consisting of two field, truncated versions
        /// of the single allele names listed in single expressing alleles phenotype.
        /// </summary>
        PhenotypeInfo<string> SixLocus_ExpressingAlleles_WithTruncatedNames { get; }

        /// <summary>
        /// A, B, C, DPB1, DQB1 & DRB1 phenotype consisting of XX Codes that include alleles listed in the
        /// 6 locus, single expressing alleles phenotype.
        /// </summary>
        PhenotypeInfo<string> SixLocus_XxCodes { get; }

        /// <summary>
        /// A, B, C, DQB1 & DRB1 phenotype (no serology available for DPB1) consisting of Serologies
        /// that map to alleles listed in the 6 locus, single expressing alleles phenotype.
        /// </summary>
        PhenotypeInfo<string> FiveLocus_Serologies { get; }
    }
}
