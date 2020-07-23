using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface INormalisedPoolRepository
    {
        Task DeleteNormalisedHaplotypeFrequencyPool();
        Task BulkInsertNormalisedHaplotypes(IReadOnlyCollection<NormalisedHaplotypeFrequency> haplotypeFrequencies);
    }

    internal class NormalisedPoolRepository : INormalisedPoolRepository
    {
        private readonly string connectionString;

        public NormalisedPoolRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteNormalisedHaplotypeFrequencyPool()
        {
            var sql = $"TRUNCATE TABLE {nameof(MatchPredictionVerificationContext.NormalisedHaplotypeFrequencyPool)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql);
            }
        }

        public async Task BulkInsertNormalisedHaplotypes(IReadOnlyCollection<NormalisedHaplotypeFrequency> haplotypeFrequencies)
        {
            if (!haplotypeFrequencies.Any())
            {
                return;
            }

            var dataTable = BuildDataTable(haplotypeFrequencies);

            using (var sqlBulk = BuildSqlBulkCopy())
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        private static DataTable BuildDataTable(IReadOnlyCollection<NormalisedHaplotypeFrequency> haplotypeFrequencies)
        {
            var dataTable = new DataTable();
            foreach (var columnName in NormalisedHaplotypeFrequency.GetColumnNames())
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var haplotypeFrequency in haplotypeFrequencies)
            {
                dataTable.Rows.Add(
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

        private SqlBulkCopy BuildSqlBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.NormalisedHaplotypeFrequencyPool)
            };

            foreach (var columnName in NormalisedHaplotypeFrequency.GetColumnNames())
            {
                // Relies on setting up the data table with column names matching the database columns.
                sqlBulk.ColumnMappings.Add(columnName, columnName);
            }

            return sqlBulk;
        }
    }
}
