using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public abstract class DonorMatchingServiceBase
    {
        protected class DonorAndMatchForLocus
        {
            public LocusMatchDetails Match { get; set; }
            public int DonorId { get; set; }
            public Locus Locus { get; set; }
        }

        protected static DonorAndMatchForLocus DonorAndMatchFromGroup(IGrouping<int, PotentialHlaMatchRelation> group, Locus locus)
        {
            var donorId = group.Key;
            var potentialHlaMatchRelations = group.ToList();
            return new DonorAndMatchForLocus
            {
                DonorId = donorId,
                Match = new LocusMatchDetails
                {
                    // TODO: ATLAS-714: Don't rely on having this ordering in two places? 
                    PositionPairs = potentialHlaMatchRelations.Select(p => (p.SearchTypePosition, p.MatchingTypePosition)).ToHashSet()
                },
                Locus = locus
            };
        }
    }
}