using System.Collections.Generic;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Repositories
{
    public interface ISolarDonorRepository
    {
        IEnumerable<RawInputDonor> SomeDonors(int maxResults);
    }
}