using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface INormalisedPoolRepository
    {
        Task<int> AddNormalisedPool(int haplotypeFrequencySetId, string dataSource);
        Task TruncateNormalisedHaplotypeFrequencies();
        Task BulkInsertNormalisedHaplotypeFrequencies(IReadOnlyCollection<NormalisedHaplotypeFrequency> haplotypeFrequencies);
    }

    public class NormalisedPoolRepository : INormalisedPoolRepository
    {
        private readonly string connectionString;

        public NormalisedPoolRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> AddNormalisedPool(int haplotypeFrequencySetId, string dataSource)
        {
            var sql = @$"
                INSERT INTO {nameof(MatchPredictionVerificationContext.NormalisedPool)}
                    ({nameof(NormalisedPool.HaplotypeFrequencySetId)}, {nameof(NormalisedPool.HaplotypeFrequenciesDataSource)})
                    VALUES(@{nameof(haplotypeFrequencySetId)}, @{nameof(dataSource)});
                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<int>(sql, new { haplotypeFrequencySetId, dataSource })).Single();
            }
        }

        public async Task TruncateNormalisedHaplotypeFrequencies()
        {
            var sql = $"TRUNCATE TABLE {nameof(MatchPredictionVerificationContext.NormalisedHaplotypeFrequencies)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql);
            }
        }

        public async Task BulkInsertNormalisedHaplotypeFrequencies(IReadOnlyCollection<NormalisedHaplotypeFrequency> haplotypeFrequencies)
        {
            if (!haplotypeFrequencies.Any())
            {
                return;
            }

            var columnNames = haplotypeFrequencies.GetColumnNamesForBulkInsert();
            var dataTable = BuildDataTable(haplotypeFrequencies, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static DataTable BuildDataTable(
            IReadOnlyCollection<NormalisedHaplotypeFrequency> haplotypeFrequencies,
            IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var haplotypeFrequency in haplotypeFrequencies)
            {
                dataTable.Rows.Add(
                    haplotypeFrequency.NormalisedPool_Id,
                    haplotypeFrequency.A,
                    haplotypeFrequency.B,
                    haplotypeFrequency.C,
                    haplotypeFrequency.DQB1,
                    haplotypeFrequency.DRB1,
                    haplotypeFrequency.Frequency,
                    haplotypeFrequency.CopyNumber);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.NormalisedHaplotypeFrequencies)
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
