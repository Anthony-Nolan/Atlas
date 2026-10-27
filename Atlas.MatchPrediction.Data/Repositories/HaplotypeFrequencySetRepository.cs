using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Context;
using Atlas.MatchPrediction.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequencySetRepository
    {
        Task<HaplotypeFrequencySet> GetActiveSet(string registry, string ethnicity);
        Task<IReadOnlyCollection<HaplotypeFrequencySet>> GetAllActiveSets();

        /// <summary>The set with the given id, active or not; null when there is none.</summary>
        /// <param name="setId">The id of the set.</param>
        /// <param name="cancellationToken">Stops the read, also while it opens a connection or waits for a retry.</param>
        Task<HaplotypeFrequencySet> GetSet(int setId, CancellationToken cancellationToken = default);

        Task<HaplotypeFrequencySet> AddSet(HaplotypeFrequencySet set);
        Task ActivateSet(int setId);

        /// <summary>
        /// Writes <paramref name="contentHash"/> to each set of <paramref name="setIds"/>, also to a set that has a hash. The
        /// import uses it with the hash of the rows that it inserted: a backfill can have hashed a set of the import while
        /// its rows were still being inserted.
        /// </summary>
        Task SetContentHash(IReadOnlyCollection<int> setIds, byte[] contentHash);

        /// <summary>
        /// Writes <paramref name="contentHash"/> to the set only if the set has no hash, so a hash that the import wrote
        /// stays. The backfill uses it.
        /// </summary>
        /// <returns>Whether the hash was written.</returns>
        Task<bool> SetContentHashIfMissing(int setId, byte[] contentHash);

        /// <summary>The ids of the sets that have no content hash: the active sets first, then by id.</summary>
        Task<IReadOnlyList<int>> GetIdsOfSetsWithNoContentHash();

        Task<HaplotypeFrequencySetContentHashCounts> GetContentHashCounts();
    }

    public class HaplotypeFrequencySetRepository : IHaplotypeFrequencySetRepository
    {
        private readonly string connectionString;
        private readonly ContextFactory contextFactory;

        // EF Context is not thread safe: In order to make requests from multi-threaded code, we cannot share a DbContext.
        // Elsewhere we use Dapper, creating a new connection for each request - this is the equivalent for EFCore.
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
                    .Where(set => set.Active && set.RegistryCode == registry && set.EthnicityCode == ethnicity)
                    .SingleOrDefaultAsync();
            }
        }

        public async Task<IReadOnlyCollection<HaplotypeFrequencySet>> GetAllActiveSets()
        {
            await using (var context = NewContext())
            {
                return await context.HaplotypeFrequencySets
                    .Where(set => set.Active)
                    .AsNoTracking()
                    .ToListAsync();
            }
        }

        /// <inheritdoc />
        public async Task<HaplotypeFrequencySet> GetSet(int setId, CancellationToken cancellationToken = default)
        {
            await using (var context = NewContext())
            {
                return await context.HaplotypeFrequencySets
                    .AsNoTracking()
                    .SingleOrDefaultAsync(set => set.Id == setId, cancellationToken);
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

        /// <inheritdoc />
        public async Task SetContentHash(IReadOnlyCollection<int> setIds, byte[] contentHash)
        {
            await using (var context = NewContext())
            {
                await context.HaplotypeFrequencySets
                    .Where(set => setIds.Contains(set.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(set => set.ContentHash, contentHash));
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetContentHashIfMissing(int setId, byte[] contentHash)
        {
            await using (var context = NewContext())
            {
                var setsUpdated = await context.HaplotypeFrequencySets
                    .Where(set => set.Id == setId && set.ContentHash == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(set => set.ContentHash, contentHash));

                return setsUpdated == 1;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<int>> GetIdsOfSetsWithNoContentHash()
        {
            await using (var context = NewContext())
            {
                return await context.HaplotypeFrequencySets
                    .Where(set => set.ContentHash == null)
                    .OrderByDescending(set => set.Active)
                    .ThenBy(set => set.Id)
                    .Select(set => set.Id)
                    .ToListAsync();
            }
        }

        /// <inheritdoc />
        public async Task<HaplotypeFrequencySetContentHashCounts> GetContentHashCounts()
        {
            await using (var context = NewContext())
            {
                var sets = context.HaplotypeFrequencySets;

                return new HaplotypeFrequencySetContentHashCounts(
                    Sets: await sets.CountAsync(),
                    SetsWithNoContentHash: await sets.CountAsync(set => set.ContentHash == null),
                    ContentHashesOfActiveSets: await sets
                        .Where(set => set.Active && set.ContentHash != null)
                        .Select(set => set.ContentHash)
                        .Distinct()
                        .CountAsync(),
                    ContentHashes: await sets
                        .Where(set => set.ContentHash != null)
                        .Select(set => set.ContentHash)
                        .Distinct()
                        .CountAsync());
            }
        }
    }
}