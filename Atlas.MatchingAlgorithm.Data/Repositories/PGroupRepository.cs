using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Services;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using MoreLinq;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public interface IPGroupRepository
    {
        /// <summary>
        /// Inserts a batch of p groups into storage.
        /// This is only relevant for relational databases (i.e. SQL), where the PGroups are stored separately to the match data.
        /// </summary>
        void InsertPGroups(IEnumerable<string> pGroups);
        Task<IEnumerable<int>> GetPGroupIds(IEnumerable<string> pGroupNames);
        Dictionary<string, int> FindOrCreatePGroupIds(List<string> allPGroups);
        int FindOrCreatePGroup(string pGroupName);
    }

    public class PGroupRepository : Repository, IPGroupRepository
    {
        private Dictionary<string, int> pGroupNameToIdDictionary;

        public PGroupRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public void InsertPGroups(IEnumerable<string> pGroups)
        {
            EnsurePGroupDictionaryCacheIsPopulated();

            var newPGroups = pGroups.Distinct().Except(pGroupNameToIdDictionary.Keys.ToList()).ToList();
            var dt = new DataTable();
            dt.Columns.Add("Id");
            dt.Columns.Add("Name");

            foreach (var pg in newPGroups)
            {
                dt.Rows.Add(0, pg);
            }

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();
                var transaction = conn.BeginTransaction();
                using (var sqlBulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, transaction))
                {
                    sqlBulk.BulkCopyTimeout = 600;
                    sqlBulk.BatchSize = 10000;
                    sqlBulk.DestinationTableName = "PGroupNames";
                    sqlBulk.WriteToServer(dt);
                }

                transaction.Commit();
                conn.Close();
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
WHERE p.Name IN ({pGroupNames.Select(name => $"'{name}'").StringJoin(", ")}) 
";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<int>(sql, commandTimeout: 300);
            }
        }

        /// <summary>
        /// Returns a dictionary of pGroups to pGroup-DB-Ids.
        /// Achieves this by reading from an in-memory cache, for the PGroups that already exist.
        /// For the PGroups that are new, creates those PGroups in the DB, and then reads their newly created IDs.
        /// </summary>
        /// <remarks>
        /// Whilst in *principle* this method can create new PGroups, we don't believe this will ever actually happen in Prod code.
        /// That's because every PGroup we see comes from expanding some other HLA, via the HMD, and thus is one of the known
        /// PGroups in the current HLA Version.
        /// But during DataRefresh we actively create DB Records for every known PGroup in the HLA Version.
        ///
        /// Thus it should not be possible for this code to receive a PGroup that isn't already in the DB ... in prod.
        ///
        /// In *TESTS* however, we don't pre-populate PGroups, so the "or create" branch of the code is necessary.
        /// </remarks>
        public Dictionary<string, int> FindOrCreatePGroupIds(List<string> allPGroups)
        {
            allPGroups = allPGroups.Distinct().ToList();
            EnsurePGroupDictionaryCacheIsPopulated();
            var (existingPGroups, newPGroups) = allPGroups.ReifyAndSplit(pGrp => pGroupNameToIdDictionary.ContainsKey(pGrp));

            InsertPGroups(newPGroups); //This method refreshes the Cache after adding.

            return allPGroups.ToDictionary(pGrp => pGrp, pGrp => pGroupNameToIdDictionary[pGrp]);
        }

        /// <remarks>
        /// See note on <see cref="FindOrCreatePGroupIds"/>.
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