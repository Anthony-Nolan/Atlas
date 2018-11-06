using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Common.Repositories
{
    public interface IDonorImportRepository
    {
        /// <summary>
        /// Insert a batch of donors into the database.
        /// This does _not_ refresh or create the hla matches.
        /// </summary>
        Task InsertBatchOfDonors(IEnumerable<InputDonor> donors);

        /// <summary>
        /// Adds pre-processed matching p-groups for a batch of donors
        /// Used when adding donors
        /// </summary>
        Task AddMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> donors);

        /// <summary>
        /// Inserts a donor and generates the matching groups.
        /// </summary>
        Task InsertDonorWithHla(InputDonorWithExpandedHla donor);

        /// <summary>
        /// Update donor information, including HLA, and refresh the pre-processed matching groups.
        /// </summary>
        Task UpdateDonorWithHla(InputDonorWithExpandedHla donor);

        /// <summary>
        /// Insert a batch of donors into the database.
        /// Will create the hla matches.
        /// </summary>
        Task InsertBatchOfDonorsWithHla(IEnumerable<InputDonorWithExpandedHla> donors);
        
        /// <summary>
        /// Insert a batch of donors into the database.
        /// Will create the hla matches.
        /// </summary>
        Task UpdateBatchOfDonorsWithHla(IEnumerable<InputDonorWithExpandedHla> donors);
    }
}