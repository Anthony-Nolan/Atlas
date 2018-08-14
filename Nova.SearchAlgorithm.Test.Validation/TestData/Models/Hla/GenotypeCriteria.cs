using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// A set of criteria used to generate a matching genotype
    /// </summary>
    public class GenotypeCriteria
    {
        public PhenotypeInfo<bool> HasNonUniquePGroups { get; set; }
    }
}