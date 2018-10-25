using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Matching;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Data.Helpers;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Models.Extensions;
using Nova.SearchAlgorithm.Repositories.Donors;

namespace Nova.SearchAlgorithm.Data.Repositories
{
    public class PGroupRepository : IPGroupRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["SqlConnectionString"].ConnectionString;

        private Dictionary<string, PGroupName> pGroupDictionary;

        public void InsertPGroups(IEnumerable<string> pGroups)
        {
            using (var conn = new SqlConnection(connectionString))
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

            string sql = @"
INSERT INTO PGroupNames (Name) VALUES (@PGroupName);
SELECT CAST(SCOPE_IDENTITY() as int)";

            int newId;

            using (var conn = new SqlConnection(connectionString))
            {
                newId = conn.Query<int>(sql, new {PGroupName = pGroupName}).Single();
            }

            CachePGroupDictionary();
            return newId;
        }

        private void CachePGroupDictionary()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                var innerPGroups = conn.Query<PGroupName>("SELECT * FROM PGroupNames");
                pGroupDictionary = innerPGroups.Distinct(new DistinctPGroupNameComparer()).ToDictionary(p => p.Name);
            }
        }
    }
}