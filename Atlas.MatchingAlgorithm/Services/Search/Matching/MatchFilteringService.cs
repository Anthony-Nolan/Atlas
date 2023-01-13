using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;

namespace Atlas.MatchingAlgorithm.Services.Search.Matching
{
    public interface IMatchFilteringService
    {
        bool IsAvailableForSearch(MatchResult match);
        bool FulfilsPerLocusMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria, Locus locus);
        bool FulfilsTotalMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsSearchTypeCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsConfigurableMatchCountCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
    }
    
    public class MatchFilteringService: IMatchFilteringService
    {
        private const int MaximumMatchCountPerLocus = 2;

        public bool IsAvailableForSearch(MatchResult match)
        {
            return match.DonorInfo.IsAvailableForSearch;
        }

        public bool FulfilsPerLocusMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria, Locus locus)
        {
            var locusMatchDetails = match.MatchDetailsForLocus(locus);
            var locusCriteria = criteria.LocusCriteria.GetLocus(locus);
            return locusMatchDetails.MatchCount >= MaximumMatchCountPerLocus - locusCriteria.MismatchCount;
        }

        public bool FulfilsTotalMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            return match.TotalMatchCount >= DesiredMatchCount(criteria);
        }

        public bool FulfilsSearchTypeCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            return match.DonorInfo.DonorType == criteria.SearchType;
        }

        /// <inheritdoc />
        public bool FulfilsConfigurableMatchCountCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            if (!criteria.ShouldIncludeBetterMatches)
            {
                return match.TotalMatchCount == DesiredMatchCount(criteria);
            }

            return true;
        }

        private static int DesiredMatchCount(AlleleLevelMatchCriteria criteria)
        {
            return criteria.LociWithCriteriaSpecified().Count() * MaximumMatchCountPerLocus - criteria.DonorMismatchCount;
        }
    }
}