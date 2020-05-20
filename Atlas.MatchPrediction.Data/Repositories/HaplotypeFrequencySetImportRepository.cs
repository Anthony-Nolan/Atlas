using Atlas.MatchPrediction.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequencySetImportRepository
    {
        /// <returns>New Id for the set.</returns>
        Task<int> AddHaplotypeFrequencySet(HaplotypeFrequencySet set);

        Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies);
    }

    public class HaplotypeFrequencySetImportRepository : IHaplotypeFrequencySetImportRepository
    {
        private string connectionString;

        public HaplotypeFrequencySetImportRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> AddHaplotypeFrequencySet(HaplotypeFrequencySet set)
        {
            throw new System.NotImplementedException();
        }

        public async Task AddHaplotypeFrequencies(int haplotypeFrequencySetId, IEnumerable<HaplotypeFrequency> haplotypeFrequencies)
        {
            throw new System.NotImplementedException();
        }
    }
}
