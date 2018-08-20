using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// A set of criteria used to generate a matching genotype
    /// </summary>
    public class GenotypeCriteria
    {
        /// <summary>
        /// Determines what length of TGS alleles are used generating the Genotype
        /// </summary>
        public PhenotypeInfo<TgsHlaTypingCategory> TgsHlaCategories { get; set; }
        
        /// <summary>
        /// Will be used to determine whether to draw test data from the specially curated dataset that allows for p-group level matching
        /// </summary>
        public PhenotypeInfo<bool> PGroupMatchPossible { get; set; }
        
        /// <summary>
        /// Will be used to determine whether to draw test data from the specially curated dataset that allows for g-group level matching
        /// </summary>
        public PhenotypeInfo<bool> GGroupMatchPossible { get; set; }
        
        public LocusInfo<bool> IsHomozygous { get; set; }
    }
}