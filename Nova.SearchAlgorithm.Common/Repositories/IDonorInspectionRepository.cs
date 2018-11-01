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
        Task<IBatchQueryAsync<DonorResult>> DonorsAddedSinceLastHlaUpdate();
        Task<DonorResult> GetDonor(int donorId);
        Task<IEnumerable<DonorIdWithPGroupNames>> GetPGroupsForDonors(IEnumerable<int> donorIds);
        Task<IEnumerable<DonorResult>> GetDonors(IEnumerable<int> donorIds);
    }
}