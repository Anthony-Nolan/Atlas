using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Common.Repositories.DonorUpdates
{
    /// <summary>
    /// Responsible for ongoing inserts / updates of donor information
    /// </summary>
    public interface IDonorUpdateRepository
    {
        /// <summary>
        /// Inserts a donor and generates the matching p-groups.
        /// </summary>
        Task InsertDonorWithExpandedHla(InputDonorWithExpandedHla donor);

        /// <summary>
        /// Insert a batch of donors into the database.
        /// Will create the hla matches.
        /// </summary>
        Task InsertBatchOfDonorsWithExpandedHla(IEnumerable<InputDonorWithExpandedHla> donors);

        /// <summary>
        /// Updates info and/or HLA for a batch of donors.
        /// </summary>
        Task UpdateDonorBatch(IEnumerable<InputDonorWithExpandedHla> donorsToUpdate);
        
        /// <summary>
        /// Sets a batch of donors as unavailable for search.
        /// </summary>
        Task SetDonorBatchAsUnavailableForSearch(IEnumerable<int> donorIds);
    }
}