using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla
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
        
        public LociInfo<bool> IsHomozygous { get; set; }
        
        /// <summary>
        /// When set, the alleles used to generate an allele string (of names) will be guaranteed to contain
        /// at least one allele with a first field different to the selected allele
        /// e.g. 01:01/02:01
        /// </summary>
        public PhenotypeInfo<bool> AlleleStringContainsDifferentAntigenGroups { get; set; }
    }
}