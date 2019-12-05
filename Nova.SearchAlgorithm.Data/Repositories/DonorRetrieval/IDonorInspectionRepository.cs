using Nova.SearchAlgorithm.Common.Models.Matching;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Data.Repositories.DonorRetrieval
{
    /// <summary>
    /// Provides methods for retrieving donor data used when returning search results. Does not perform any filtering of donors
    /// </summary>
    public interface IDonorInspectionRepository
    {
        Task<InputDonor> GetDonor(int donorId);
        Task<Dictionary<int, InputDonor>> GetDonors(IEnumerable<int> donorIds);
        Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds);
    }
}