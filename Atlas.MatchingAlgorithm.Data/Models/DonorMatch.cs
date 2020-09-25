using System.Collections.Generic;
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
    
    public class FullDonorMatch
    {
        public int DonorId { get; set; }
        public int? TypePosition1 { get; set; }

        public int? TypePosition2 { get; set; }

        internal async IAsyncEnumerable<PotentialHlaMatchRelation> ToPotentialHlaMatchRelations(Locus locus)
        {
            if (TypePosition1 != null)
            {
                yield return new PotentialHlaMatchRelation
                {
                    Locus = locus,
                    Name = "Unknown",
                    SearchTypePosition = TypePosition.One.ToLocusPosition(),
                    MatchingTypePosition = ((TypePosition) TypePosition1).ToLocusPosition(),
                    DonorId = DonorId
                };
            }

            if (TypePosition2 != null)
            {
                yield return new PotentialHlaMatchRelation
                {
                    Locus = locus,
                    Name = "Unknown",
                    SearchTypePosition = TypePosition.Two.ToLocusPosition(),
                    MatchingTypePosition = ((TypePosition) TypePosition2).ToLocusPosition(),
                    DonorId = DonorId
                };
            }
        }
    }
}