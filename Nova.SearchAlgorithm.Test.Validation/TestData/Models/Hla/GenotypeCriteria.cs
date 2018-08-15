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
        
        public PhenotypeInfo<bool> HasNonUniquePGroups { get; set; }
    }
}