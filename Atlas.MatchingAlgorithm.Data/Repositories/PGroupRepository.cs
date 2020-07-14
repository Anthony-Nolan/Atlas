using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Helpers;
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
                newId = conn.Query<int>(sql, new {PGroupName = pGroupName}, commandTimeout: 300).Single();
            }

            pGroupNameToIdDictionary.Add(pGroupName, newId);
            return newId;
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