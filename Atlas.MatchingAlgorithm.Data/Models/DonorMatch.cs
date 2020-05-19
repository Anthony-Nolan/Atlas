using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Data.Models
{
    public class DonorMatch
    {
        public int DonorId { get; set; }
        public int TypePosition { get; set; }
        
        internal PotentialHlaMatchRelation ToPotentialHlaMatchRelation(TypePosition searchTypePosition, Locus locus)
        {
            return new PotentialHlaMatchRelation()
            {
                Locus = locus,
                Name = "Unknown",
                SearchTypePosition = searchTypePosition.ToLocusPosition(),
                MatchingTypePosition = ((TypePosition) TypePosition).ToLocusPosition(),
                DonorId = DonorId
            };
        }
    }

}