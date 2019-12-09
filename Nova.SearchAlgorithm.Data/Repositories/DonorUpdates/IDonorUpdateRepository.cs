using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Data.Repositories.DonorUpdates
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
        Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<DonorInfoWithExpandedHla> donors);

        /// <summary>
        /// Updates info and/or HLA for a batch of donors.
        /// </summary>
        Task UpdateDonorBatch(IEnumerable<DonorInfoWithExpandedHla> donorsToUpdate);
        
        /// <summary>
        /// Sets a batch of donors as unavailable for search.
        /// </summary>
        Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds);
    }
}