using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors
{
    public interface IDonorDocumentStorage
    {
        /// <summary>
        /// Inserts the raw donor but does not insert any match data
        /// </summary>
        Task InsertDonor(RawInputDonor donor);
        Task InsertBatchOfDonors(IEnumerable<RawInputDonor> donors);
        
        /// <summary>
        /// Performs one time set up before the Refresh Hla function is run
        /// e.g. generating a new data table in the azure table storage implementation
        /// </summary>
        void SetupForHlaRefresh();
        
        /// <summary>
        /// Refreshes or creates the match data based on the donor's (new)
        /// expanded HLA information.
        /// </summary>
        Task RefreshMatchingGroupsForExistingDonor(InputDonor donor);
        
        Task<DonorResult> GetDonor(int donorId);
        IBatchQueryAsync<DonorResult> AllDonors();
        Task<IEnumerable<PotentialHlaMatchRelation>> GetDonorMatchesAtLocus(Locus locus, LocusSearchCriteria criteria);
        Task<int> HighestDonorId();
    }
}