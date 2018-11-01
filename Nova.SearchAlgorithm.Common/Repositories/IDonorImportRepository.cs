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
        /// If a donor with the given DonorId already exists, update the HLA and refresh the pre-processed matching groups.
        /// Otherwise, insert the donor and generate the matching groups.
        /// </summary>
        Task AddOrUpdateDonorWithHla(InputDonorWithExpandedHla donor);
        
        /// <summary>
        /// Adds pre-processed matching p-groups for a batch of donors
        /// Used when adding donors
        /// </summary>
        Task AddMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> donors);
        
        /// <summary>
        /// Re-calculates and replaces pre-processed matching p-groups for a batch of donors
        /// Used when updating donor hla information
        /// </summary>
        Task ReplaceMatchingGroupsForExistingDonorBatch(IEnumerable<InputDonorWithExpandedHla> donors);
    }
}