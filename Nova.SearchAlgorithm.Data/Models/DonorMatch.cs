using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class DonorMatch
    {
        public int DonorId { get; set; }
        public int TypePosition { get; set; }
        
        public PotentialHlaMatchRelation ToPotentialHlaMatchRelation(TypePositions searchTypePosition, Locus locus)
        {
            return new PotentialHlaMatchRelation()
            {
                Locus = locus,
                Name = "Unknown",
                SearchTypePosition = searchTypePosition,
                MatchingTypePositions = (TypePositions) TypePosition,
                DonorId = DonorId
            };
        }
    }

}