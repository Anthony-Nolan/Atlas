using Atlas.MatchPrediction.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesRepository
    {
       Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);
    }

    public class HaplotypeFrequenciesRepository : IHaplotypeFrequenciesRepository
    {
        private string connectionString;

        public HaplotypeFrequenciesRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            throw new System.NotImplementedException();
        }
    }
}
