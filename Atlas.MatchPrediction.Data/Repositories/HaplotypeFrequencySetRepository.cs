using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequencySetRepository
    {
        Task<HaplotypeFrequencySet> GetActiveSet(string registry, string ethnicity);
        Task<HaplotypeFrequencySet> AddSet(HaplotypeFrequencySet set);
        Task ActivateSet(int setId);
    }

    public class HaplotypeFrequencySetRepository : IHaplotypeFrequencySetRepository
    {
        private readonly string connectionString;
        private readonly ContextFactory contextFactory;

        private MatchPredictionContext NewContext() => contextFactory.Create(connectionString);

        public HaplotypeFrequencySetRepository(string connectionString, ContextFactory contextFactory)
        {
            this.connectionString = connectionString;
            this.contextFactory = contextFactory;
        }

        public async Task<HaplotypeFrequencySet> GetActiveSet(string registry, string ethnicity)
        {
            await using (var context = NewContext())
            {
                return await context.HaplotypeFrequencySets
                    .Where(set => set.Active == true && set.RegistryCode == registry && set.EthnicityCode == ethnicity)
                    .SingleOrDefaultAsync();
            }
        }

        public async Task<HaplotypeFrequencySet> AddSet(HaplotypeFrequencySet set)
        {
            await using (var context = NewContext())
            {
                await context.HaplotypeFrequencySets.AddAsync(set);
                await context.SaveChangesAsync();
                return set;
            }
        }

        public async Task ActivateSet(int setId)
        {
            await using (var context = NewContext())
            {
                var set = await context.HaplotypeFrequencySets.SingleAsync(s => s.Id == setId);
                set.Active = true;

                var otherMatchingSets = context.HaplotypeFrequencySets.Where(s =>
                    s.Id != setId
                    && s.EthnicityCode == set.EthnicityCode
                    && s.RegistryCode == set.RegistryCode
                );

                foreach (var otherMatchingSet in otherMatchingSets)
                {
                    otherMatchingSet.Active = false;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}