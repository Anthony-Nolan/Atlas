using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// A set of criteria used to generate a matching genotype
    /// </summary>
    public class GenotypeCriteria
    {
        /// <summary>
        /// Determines which dataset to draw each allele from
        /// </summary>
        public PhenotypeInfo<Dataset> AlleleSources { get; set; }
        
        public LocusInfo<bool> IsHomozygous { get; set; }
    }
}