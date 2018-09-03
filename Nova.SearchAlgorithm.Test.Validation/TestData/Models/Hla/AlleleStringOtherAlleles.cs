using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// Constains lists of alleles to be used when generating allele strings for a given test data allele
    /// </summary>
    public class AlleleStringOtherAlleles
    {
        /// <summary>
        /// Dictates other alleles to include in an allele string (of names) representation of this TGS allele
        /// </summary>
        public IEnumerable<AlleleTestData> NameString { get; set; } = new List<AlleleTestData>();

        /// <summary>
        /// Dictates other alleles to include in an allele string (of subtypes) representation of this TGS allele
        /// </summary>
        public IEnumerable<AlleleTestData> SubtypeString { get; set; } = new List<AlleleTestData>();
        
        public IEnumerable<AlleleTestData> NameStringWithSinglePGroup { get; set; } = new List<AlleleTestData>();
        public IEnumerable<AlleleTestData> NameStringWithMultiplePGroups { get; set; } = new List<AlleleTestData>();
    }
}