using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Matching;

namespace Nova.SearchAlgorithm.Common.Repositories
{
    public interface IDonorInspectionRepository
    {
        Task<int> HighestDonorId();
        IBatchQueryAsync<DonorResult> AllDonors();
        Task<DonorResult> GetDonor(int donorId);
        Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds);
    }
}