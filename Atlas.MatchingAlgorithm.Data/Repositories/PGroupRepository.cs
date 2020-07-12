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

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    public class PGroupRepository : Repository, IPGroupRepository
    {
        private Dictionary<string, PGroupName> pGroupDictionary;

        public PGroupRepository(IConnectionStringProvider connectionStringProvider) : base(connectionStringProvider)
        {
        }

        public void InsertPGroups(IEnumerable<string> pGroups)
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                conn.Open();

                var existingPGroups = conn.Query<PGroupName>("SELECT * FROM PGroupNames").Select(p => p.Name);

                var dt = new DataTable();
                dt.Columns.Add("Id");
                dt.Columns.Add("Name");

                foreach (var pg in pGroups.Distinct().Except(existingPGroups))
                {
                    dt.Rows.Add(0, pg);
                }

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

            CachePGroupDictionary();
        }

        public int FindOrCreatePGroup(string pGroupName)
        {
            if (pGroupDictionary == null)
            {
                CachePGroupDictionary();
            }

            pGroupDictionary.TryGetValue(pGroupName, out var existing);

            if (existing != null)
            {
                return existing.Id;
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

            CachePGroupDictionary(); //QQ Umm ... what?
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

        private void CachePGroupDictionary()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var innerPGroups = conn.Query<PGroupName>("SELECT * FROM PGroupNames", commandTimeout: 300);
                pGroupDictionary = innerPGroups.Distinct(new DistinctPGroupNameComparer()).ToDictionary(p => p.Name);
            }
        }
    }
}