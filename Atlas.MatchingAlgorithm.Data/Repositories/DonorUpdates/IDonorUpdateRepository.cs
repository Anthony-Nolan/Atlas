using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates
{
    /// <summary>
    /// Responsible for ongoing inserts / updates of donor information
    /// </summary>
    public interface IDonorUpdateRepository
    {
        /// <summary>
        /// Insert a batch of donors into the database.
        /// Will create the hla matches.
        /// </summary>
        Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<DonorInfoWithExpandedHla> donors, bool runAllHlaInsertionsInASingleTransactionScope);

        /// <summary>
        /// Updates info and/or HLA for a batch of donors.
        /// </summary>
        Task UpdateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donorsToUpdate, bool runAllHlaInsertionsInASingleTransactionScope);
        
        /// <summary>
        /// Sets a batch of donors as unavailable for search.
        /// </summary>
        Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds);
    }
}