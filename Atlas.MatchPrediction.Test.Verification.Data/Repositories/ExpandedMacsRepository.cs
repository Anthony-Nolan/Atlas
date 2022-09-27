using Atlas.MatchPrediction.Test.Verification.Data.Context;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities;
using Atlas.Common.Sql.BulkInsert;

namespace Atlas.MatchPrediction.Test.Verification.Data.Repositories
{
    public interface IExpandedMacsRepository
    {
        Task<string> GetLastCodeInserted();
        Task DeleteCode(string code);
        Task BulkInsert(IReadOnlyCollection<ExpandedMac> macs);
        Task<IEnumerable<string>> SelectCodesBySecondField(string secondField);
        Task<IEnumerable<string>> SelectSecondFieldsByCode(string code);
    }

    public class ExpandedMacsRepository : IExpandedMacsRepository
    {
        private readonly string connectionString;

        public ExpandedMacsRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<string> GetLastCodeInserted()
        {
            var sql = @$"SELECT TOP 1 Code FROM ExpandedMacs ORDER BY Id DESC";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<string>(sql)).SingleOrDefault();
            }
        }

        public async Task DeleteCode(string code)
        {
            var sql = @$"DELETE FROM ExpandedMacs WHERE Code = @{nameof(code)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.ExecuteAsync(sql, new { code });
            }
        }

        public async Task BulkInsert(IReadOnlyCollection<ExpandedMac> macs)
        {
            if (!macs.Any())
            {
                return;
            }

            var columnNames = macs.GetColumnNamesForBulkInsert();
            var dataTable = BuildDataTable(macs, columnNames);

            using (var sqlBulk = BuildSqlBulkCopy(columnNames))
            {
                await sqlBulk.WriteToServerAsync(dataTable);
            }
        }

        public async Task<IEnumerable<string>> SelectCodesBySecondField(string secondField)
        {
            var sql = @$"SELECT Code FROM ExpandedMacs WHERE SecondField = @{nameof(secondField)} ORDER BY Code";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<string>(sql, new { secondField });
            }
        }

        public async Task<IEnumerable<string>> SelectSecondFieldsByCode(string code)
        {
            var sql = @$"SELECT SecondField FROM ExpandedMacs WHERE Code = @{nameof(code)} ORDER BY SecondField";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<string>(sql, new { code });
            }
        }

        private static DataTable BuildDataTable(IReadOnlyCollection<ExpandedMac> macs, IEnumerable<string> columnNames)
        {
            var dataTable = new DataTable();
            foreach (var columnName in columnNames)
            {
                dataTable.Columns.Add(columnName);
            }

            foreach (var mac in macs)
            {
                dataTable.Rows.Add(mac.SecondField, mac.Code);
            }

            return dataTable;
        }

        private SqlBulkCopy BuildSqlBulkCopy(IEnumerable<string> columnNames)
        {
            var sqlBulk = new SqlBulkCopy(connectionString)
            {
                BulkCopyTimeout = 3600,
                BatchSize = 10000,
                DestinationTableName = nameof(MatchPredictionVerificationContext.ExpandedMacs)
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
