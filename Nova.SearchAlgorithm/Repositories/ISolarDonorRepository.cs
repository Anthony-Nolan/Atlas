using System.Collections.Generic;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Repositories
{
    public interface ISolarDonorRepository
    {
        IEnumerable<RawInputDonor> SomeDonors(int maxResults);
    }
}