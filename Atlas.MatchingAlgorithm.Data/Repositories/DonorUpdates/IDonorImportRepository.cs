using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    /// <summary>
    /// Responsible for one-off, full donor imports
    /// </summary>
    public interface IDonorImportRepository
    {
        /// <summary>
        /// Performs any upfront work necessary to run a full donor import with reasonable performance.
        /// e.g. Removing indexes in a SQL implementation
        /// </summary>
        Task FullHlaRefreshSetUp();
        
        /// <summary>
        /// Performs any work necessary after a full donor import has been run.
        /// e.g. Re-adding indexes in a SQL implementation
        /// </summary>
        Task FullHlaRefreshTearDown();

        /// <summary>
        /// Removes all donors, and all p-group data
        /// </summary>
        /// <returns></returns>
        Task RemoveAllDonorInformation();

        /// <summary>
        /// Insert a batch of donors into the database.
        /// This does _not_ refresh or create the hla matches.
        /// </summary>
        Task InsertBatchOfDonors(IEnumerable<DonorInfo> donors);

        /// <summary>
        /// Adds pre-processed matching p-groups for a batch of donors
        /// Used when adding donors
        /// </summary>
        Task AddMatchingPGroupsForExistingDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donors);

        Task RemovePGroupsForDonorBatch(IEnumerable<int> donorIds);
    }
}