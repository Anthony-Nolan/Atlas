using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;

namespace Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval
{
    /// <summary>
    /// Provides methods for retrieving donor data used when returning search results. Does not perform any filtering of donors
    /// </summary>
    public interface IDonorInspectionRepository
    {
        Task<DonorInfo> GetDonor(int donorId);
        Task<Dictionary<int, DonorInfo>> GetDonors(IEnumerable<int> donorIds);
        Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds);
    }
}