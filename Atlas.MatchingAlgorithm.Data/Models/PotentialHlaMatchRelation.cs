using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Data.Models
{
    /// <summary>
    /// An entity to store the relationship between hla (key) and donor ids (value)
    /// </summary>
    public class PotentialHlaMatchRelation
    {
        public Locus Locus { get; set; }
        public LocusPosition SearchTypePosition { get; set; }
        public LocusPosition MatchingTypePosition { get; set; }
        public string Name { get; set; }
        public int DonorId { get; set; }
    }
}
