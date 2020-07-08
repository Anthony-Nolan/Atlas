using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Models.Hla
{
    /// <summary>
    /// Constains lists of alleles to be used when generating allele strings for a given test data allele
    /// </summary>
    public class AlleleStringOptions
    {
        /// <summary>
        /// Dictates other alleles to include in an allele string (of names) representation of this TGS allele
        /// </summary>
        public List<AlleleTestData> NameString { get; set; } = new List<AlleleTestData>();

        /// <summary>
        /// Dictates other alleles to include in an allele string (of subtypes) representation of this TGS allele
        /// </summary>
        public List<AlleleTestData> SubtypeString { get; set; } = new List<AlleleTestData>();
        
        public List<AlleleTestData> NameStringWithSinglePGroup { get; set; } = new List<AlleleTestData>();
        public List<AlleleTestData> NameStringWithMultiplePGroups { get; set; } = new List<AlleleTestData>();
    }
}