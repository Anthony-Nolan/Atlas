using System.Collections.Generic;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories
{
    public interface ISolarDonorRepository
    {
        Task<IEnumerable<RawInputDonor>> SomeDonors(int maxResults, int lastId = 0);
    }
}