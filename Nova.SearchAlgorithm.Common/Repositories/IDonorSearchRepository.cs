using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Common.Repositories
{
    public interface IDonorSearchRepository
    {
        Task<IEnumerable<PotentialSearchResult>> Search(AlleleLevelMatchCriteria matchRequest);
    }
}