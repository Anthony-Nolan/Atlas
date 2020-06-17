using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    public interface IDonorSearchPhaseOneRepository
    {
        /// <summary>
        /// Returns donor matches at a given locus matching the search criteria
        /// </summary>
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions
        );
    }

    public interface IDonorSearchPhaseTwoRepository
    {
        /// <summary>
        /// Returns donor matches at a given locus matching the search criteria, that are also present in a supplied list of donor ids
        /// </summary>
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocusFromDonorSelection(
            Locus locus,
            LocusSearchCriteria criteria,
            IEnumerable<int> donorIds
        );
    }
}