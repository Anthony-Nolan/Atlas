using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.TestHarness;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface ISimulantsRepository
    {
        Task BulkInsertSimulants(IReadOnlyCollection<Simulant> simulants);
        Task<IEnumerable<Simulant>> GetGenotypeSimulants(int testHarnessId, string testIndividualCategory);
        
        /// <returns>Genotypes and phenotypes belonging to all simulants of type <paramref name="testIndividualCategory"/>
        /// within test harness of id, <paramref name="testHarnessId"/>.</returns>
        Task<IEnumerable<Simulant>> GetSimulants(int testHarnessId, string testIndividualCategory);
    }

    public class SimulantsRepository : ISimulantsRepository
    {
        private readonly string connectionString;

        public SimulantsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<Simulant>> GetGenotypeSimulants(int testHarnessId, string testIndividualCategory)
        {
            var sql = @$"SELECT s.* FROM Simulants s WHERE 
                    s.TestHarness_Id = @{nameof(testHarnessId)} AND
                    s.TestIndividualCategory = @{nameof(testIndividualCategory)} AND
                    s.SimulatedHlaTypingCategory = '{SimulatedHlaTypingCategory.Genotype}'";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<Simulant>(sql, new { testHarnessId, testIndividualCategory });
            }
        }

        public async Task<IEnumerable<Simulant>> GetSimulants(int testHarnessId, string testIndividualCategory)
        {
            var sql = @$"SELECT s.* FROM Simulants s WHERE 
                    s.TestHarness_Id = @{nameof(testHarnessId)} AND
                    s.TestIndividualCategory = @{nameof(testIndividualCategory)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<Simulant>(sql, new { testHarnessId, testIndividualCategory });
            }
        }

        public async Task BulkInsertSimulants(IReadOnlyCollection<Simulant> simulants)
        {
            if (!simulants.Any())
            {
                return;
            }

            var columnNames = simulants.GetColumnNamesForBulkInsert();
            var dataTable = BuildDataTable(simulants, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static DataTable BuildDataTable(IReadOnlyCollection<Simulant> simulants, IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var simulant in simulants)
            {
                dataTable.Rows.Add(
                    simulant.TestHarness_Id,
                    simulant.TestIndividualCategory,
                    simulant.SimulatedHlaTypingCategory,
                    simulant.A_1,
                    simulant.A_2,
                    simulant.B_1,
                    simulant.B_2,
                    simulant.C_1,
                    simulant.C_2,
                    simulant.DQB1_1,
                    simulant.DQB1_2,
                    simulant.DRB1_1,
                    simulant.DRB1_2,
                    simulant.SourceSimulantId);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.Simulants)
            };

            foreach (var columnName in columnNames)
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }

            return sqlBulk;
        }
    }
}
