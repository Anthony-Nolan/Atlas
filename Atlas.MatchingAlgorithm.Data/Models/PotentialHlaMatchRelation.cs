using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Data.Models
{
    /// <summary>
    /// An entity to store the relationship between hla (key) and donor ids (value)
    /// </summary>
    public class PotentialHlaMatchRelation
    {
        public Locus Locus { get; set; }
        public TypePosition SearchTypePosition { get; set; }
        public TypePosition MatchingTypePosition { get; set; }
        public string Name { get; set; }
        public int DonorId { get; set; }
        public DonorInfoWithExpandedHla Donor { get; set; }
    }
}
