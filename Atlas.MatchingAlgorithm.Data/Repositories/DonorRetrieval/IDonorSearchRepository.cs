using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Models;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    /// <summary>
    /// Fetches filtered donors, based on a set of matching criteria
    /// </summary>
    public interface IDonorSearchRepository
    {
        /// <summary>
        /// Returns donor matches at a given locus matching the search criteria
        /// </summary>
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(
            Locus locus,
            LocusSearchCriteria criteria,
            MatchingFilteringOptions filteringOptions
        );

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