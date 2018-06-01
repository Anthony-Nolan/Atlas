using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    /// <summary>
    /// An entity to store the relationship between hla (key) and donor ids (value)
    /// </summary>
    public class PotentialHlaMatchRelation
    {
        public Locus Locus { get; set; }
        public TypePositions SearchTypePosition { get; set; }
        public TypePositions MatchingTypePositions { get; set; }
        public string Name { get; set; }

        public int DonorId { get; set; }
    }
}
