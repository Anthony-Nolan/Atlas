using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Sql;
using Atlas.MatchingAlgorithm.Data.Helpers;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using LoggingStopwatch;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IPGroupRepository
    {
        /// <summary>
        /// Given a collection of PGroups, inserts any that don't already exist into the Database, and updates the in-memory map to their Ids
        /// </summary>
        Task InsertPGroups(IEnumerable<string> pGroups);
        Task<IEnumerable<int>> GetPGroupIds(IEnumerable<string> pGroupNames);
        Task<IDictionary<string, int>> EnsureAllPGroupsExist(IList<string> allPGroups, LongStopwatchCollection timerCollection = null);
        int FindOrCreatePGroup(string pGroupName);
    }

    public class PGroupRepository : Repository, IPGroupRepository
    {
        private IDictionary<string, int> pGroupNameToIdDictionary;

        public PGroupRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public async Task InsertPGroups(IEnumerable<string> pGroups)
        {
            EnsurePGroupDictionaryCacheIsPopulated();

            var newPGroups = pGroups.Distinct().Except(pGroupNameToIdDictionary.Keys).ToList();

            if (!newPGroups.Any())
            {
                //Nothing needs doing.
                return;
            }

            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");

            foreach (var pg in newPGroups)
            {
                dt.Rows.Add(0, pg);
            }

            using (var sqlBulk = new SqlBulkCopy(
                ConnectionStringProvider.GetConnectionString(), 
                SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction))
            {
                sqlBulk.BulkCopyTimeout = 600;
                sqlBulk.BatchSize = 10000;
                sqlBulk.DestinationTableName = "PGroupNames";
                await sqlBulk.WriteToServerAsync(dt);
            }
            // We need to get the new Ids back out.
            ForceCachePGroupDictionary();
        }

        public async Task<IEnumerable<int>> GetPGroupIds(IEnumerable<string> pGroupNames)
        {
            pGroupNames = pGroupNames.ToList();
            if (!pGroupNames.Any())
            {
                return new List<int>();
            }

            var sql = $@"
SELECT p.Id FROM PGroupNames p
WHERE p.Name IN {pGroupNames.ToInClause()} 
";

            await using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<int>(sql, commandTimeout: 600);
            }
        }

        /// <summary>
        /// For the PGroups that are new, creates those PGroups in the DB, and then reads their newly created IDs, to populate the in-memory cache.
        /// </summary>
        /// <remarks>
        /// Whilst in *principle* this method can create new PGroups, we don't believe this will ever actually happen in Prod code.
        /// That's because every PGroup we see comes from expanding some other HLA, via the HMD, and thus is one of the known
        /// PGroups in the current HLA Version.
        /// But during DataRefresh we actively create DB Records for every known PGroup in the HLA Version.
        ///
        /// Thus it should not be possible for this code to receive a PGroup that isn't already in the DB ... in prod.
        /// In *TESTS* however, we don't pre-populate PGroups, so the "or create" branch of the code is necessary.
        ///
        /// **Note that this method gets used *heavily* during DataRefresh (despite being a no-op!) and has been aggressively optimised for that use-case.**
        /// Over the course of a 2M donor import, we must check ~1B pGroup strings, which ends up taking 2-3 minutes. All in the actual dictionary lookup line.
        /// </remarks>
        public async Task<IDictionary<string, int>> EnsureAllPGroupsExist(
            IList<string> allPGroups,
            LongStopwatchCollection timerCollection = null)
        {
            EnsurePGroupDictionaryCacheIsPopulated();

            var dictionaryCheckTimer = timerCollection?.TimeInnerOperation(DataRefreshTimingKeys.NewPGroupInsertion_FindNew_TimerKey);
            // Note that it turns out that it's quicker to run this WITHOUT a .Distinct() in it.
            var newPGroups = allPGroups.Where(pGrp => !pGroupNameToIdDictionary.ContainsKey(pGrp)).ToList();
            dictionaryCheckTimer?.Dispose();

            if (newPGroups.Any())
            {
                await InsertPGroups(newPGroups); //This method refreshes the Cache after adding.
            }

            return pGroupNameToIdDictionary;
        }

        /// <remarks>
        /// See note on <see cref="EnsureAllPGroupsExist"/>.
        /// We don't think the "OrCreate" branch of this code gets used in prod - only in Tests.
        /// </remarks>
        public int FindOrCreatePGroup(string pGroupName)
        {
            EnsurePGroupDictionaryCacheIsPopulated();

            if (pGroupNameToIdDictionary.TryGetValue(pGroupName, out var existingId))
            {
                return existingId;
            }

            const string sql = @"
INSERT INTO PGroupNames (Name) VALUES (@PGroupName);
SELECT CAST(SCOPE_IDENTITY() as int)
";

            int newId;

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                newId = conn.Query<int>(sql, new { PGroupName = pGroupName }, commandTimeout: 300).Single();
            }

            pGroupNameToIdDictionary.Add(pGroupName, newId);
            return newId;
        }

        private void EnsurePGroupDictionaryCacheIsPopulated()
        {
            if (pGroupNameToIdDictionary == null)
            {
                ForceCachePGroupDictionary();
            }
        }

        private void ForceCachePGroupDictionary()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var innerPGroups = conn.Query<PGroupName>($"SELECT {nameof(PGroupName.Id)}, {nameof(PGroupName.Name)} FROM PGroupNames", commandTimeout: 300);
                pGroupNameToIdDictionary = innerPGroups.DistinctBy(pGrp => pGrp.Name).ToDictionary(p => p.Name, pGrp => pGrp.Id);
            }
        }
    }
}