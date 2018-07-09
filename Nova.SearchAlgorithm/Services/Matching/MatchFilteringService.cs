using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IMatchFilteringService
    {
        bool FulfilsPerLocusMatchCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria, Locus locus);
        bool FulfilsTotalMatchCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsSearchTypeCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsRegistryCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria);
    }
    
    public class MatchFilteringService: IMatchFilteringService
    {
        public bool FulfilsPerLocusMatchCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria, Locus locus)
        {
            var locusMatchDetails = match.MatchDetailsForLocus(locus);
            var locusCriteria = criteria.MatchCriteriaForLocus(locus);
            const int maximumMatchCount = 2;
            return locusMatchDetails.MatchCount >= maximumMatchCount - locusCriteria.MismatchCount;
        }

        public bool FulfilsTotalMatchCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria)
        {
            return match.TotalMatchCount >= (match.PopulatedLociCount * 2) - criteria.DonorMismatchCount;
        }

        public bool FulfilsSearchTypeCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria)
        {
            // TODO: NOVA-1325: Implement filtering on donor type
            throw new System.NotImplementedException();
        }

        public bool FulfilsRegistryCriteria(PotentialSearchResult match, AlleleLevelMatchCriteria criteria)
        {
            // TODO: NOVA-1326: Implement filtering on regitsry
            throw new System.NotImplementedException();
        }
    }
}