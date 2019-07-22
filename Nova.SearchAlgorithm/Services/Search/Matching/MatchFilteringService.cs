using System;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;

namespace Nova.SearchAlgorithm.Services.Matching
{
    public interface IMatchFilteringService
    {
        bool FulfilsPerLocusMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria, Locus locus);
        bool FulfilsTotalMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsSearchTypeCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsRegistryCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
        bool FulfilsSearchTypeSpecificCriteria(MatchResult match, AlleleLevelMatchCriteria criteria);
    }
    
    public class MatchFilteringService: IMatchFilteringService
    {
        private const int MaximumMatchCountPerLocus = 2;

        public bool FulfilsPerLocusMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria, Locus locus)
        {
            var locusMatchDetails = match.MatchDetailsForLocus(locus);
            var locusCriteria = criteria.MatchCriteriaForLocus(locus);
            return locusMatchDetails.MatchCount >= MaximumMatchCountPerLocus - locusCriteria.MismatchCount;
        }

        public bool FulfilsTotalMatchCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            return match.TotalMatchCount >= (match.PopulatedLociCount * MaximumMatchCountPerLocus) - criteria.DonorMismatchCount;
        }

        public bool FulfilsSearchTypeCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            return match.Donor.DonorType == criteria.SearchType;
        }

        public bool FulfilsRegistryCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            return criteria.RegistriesToSearch.Contains(match.Donor.RegistryCode);
        }

        /// <summary>
        /// The matching rules are subtly different for adult and cord searches.
        /// This method will apply any search type specific matching rules
        /// </summary>
        public bool FulfilsSearchTypeSpecificCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            switch (criteria.SearchType)
            {
                case DonorType.Adult:
                    return FulfilsAdultSpecificCriteria(match, criteria);
                case DonorType.Cord:
                    return FulfilsCordSpecificCriteria(match, criteria);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool FulfilsCordSpecificCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            // There are no cord specific matching rules.
            // Cord searches should return matches with mismatch count <= TotalMismatchCount, which is the default shared behaviour
            return true;
        }

        private static bool FulfilsAdultSpecificCriteria(MatchResult match, AlleleLevelMatchCriteria criteria)
        {
            // Adult searches should return matches only where the mismatch count equals exactly the requested mismatch count
            return match.TotalMatchCount == (match.PopulatedLociCount * MaximumMatchCountPerLocus) - criteria.DonorMismatchCount;
        }
    }
}