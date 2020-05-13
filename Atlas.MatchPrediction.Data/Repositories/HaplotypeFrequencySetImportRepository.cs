using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequencySetImportRepository
    {
        Task InsertHaplotypeFrequencySet(HaplotypeFrequencySet haplotypeFrequencySet);
    }

    public class HaplotypeFrequencySetImportRepository : IHaplotypeFrequencySetImportRepository
    {
        private readonly MatchPredictionContext context;

        public HaplotypeFrequencySetImportRepository(MatchPredictionContext context)
        {
            this.context = context;
        }

        public async Task InsertHaplotypeFrequencySet(HaplotypeFrequencySet haplotypeFrequencySet)
        {
            context.HaplotypeFrequencySets.Add(haplotypeFrequencySet);
            await context.SaveChangesAsync();
        }
    }
}
