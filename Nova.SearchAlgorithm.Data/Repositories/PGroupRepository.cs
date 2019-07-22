using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Services;

namespace Nova.SearchAlgorithm.Data.Repositories
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
                newId = conn.Query<int>(sql, new {PGroupName = pGroupName}).Single();
            }

            CachePGroupDictionary();
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
WHERE p.Name IN ('{string.Join("', '", pGroupNames)}') 
";

            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                return await conn.QueryAsync<int>(sql);
            }
        }

        private void CachePGroupDictionary()
        {
            using (var conn = new SqlConnection(ConnectionStringProvider.GetConnectionString()))
            {
                var innerPGroups = conn.Query<PGroupName>("SELECT * FROM PGroupNames");
                pGroupDictionary = innerPGroups.Distinct(new DistinctPGroupNameComparer()).ToDictionary(p => p.Name);
            }
        }
    }
}