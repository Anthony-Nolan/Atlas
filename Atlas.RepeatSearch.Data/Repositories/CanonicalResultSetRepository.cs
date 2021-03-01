using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.RepeatSearch.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.RepeatSearch.Data.Repositories
{
    public interface ICanonicalResultSetRepository
    {
        Task CreateCanonicalResultSet(string searchRequestId, IReadOnlyCollection<int> donorIds);
    }

    public class CanonicalResultSetRepository : ICanonicalResultSetRepository
    {
        private readonly string connectionString;

        public CanonicalResultSetRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task CreateCanonicalResultSet(string searchRequestId, IReadOnlyCollection<int> donorIds)
        {
            using (var transactionScope = new AsyncTransactionScope())
            {
                var resultSetId = await CreateResultSetEntity(searchRequestId);
                var resultEntryDataTable = BuildResultsInsertDataTable(resultSetId, donorIds);
                using (var sqlBulk = BuildResultsBulkCopy())
                {
                    await sqlBulk.WriteToServerAsync(resultEntryDataTable);
                }
                transactionScope.Complete();
            }
        }

        private async Task<int> CreateResultSetEntity(string searchRequestId)
        {
            var sql = @$"
INSERT INTO {CanonicalResultSet.QualifiedTableName}
({nameof(CanonicalResultSet.OriginalSearchRequestId)})
VALUES(@{nameof(searchRequestId)});

SELECT CAST(SCOPE_IDENTITY() as int);
";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleAsync<int>(sql, new {searchRequestId});
            }
        }

        private SqlBulkCopy BuildResultsBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(connectionString) {BatchSize = 10000, DestinationTableName = SearchResult.QualifiedTableName};
            sqlBulk.ColumnMappings.Add(nameof(SearchResult.Id), nameof(SearchResult.Id));
            sqlBulk.ColumnMappings.Add(nameof(SearchResult.AtlasDonorId), nameof(SearchResult.AtlasDonorId));
            sqlBulk.ColumnMappings.Add(nameof(SearchResult.CanonicalResultSetId), nameof(SearchResult.CanonicalResultSetId));
            return sqlBulk;
        }

        private DataTable BuildResultsInsertDataTable(int canonicalResultSetId, IEnumerable<int> donorIds)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(SearchResult.Id));
            dataTable.Columns.Add(nameof(SearchResult.AtlasDonorId));
            dataTable.Columns.Add(nameof(SearchResult.CanonicalResultSetId));

            foreach (var donorId in donorIds)
            {
                dataTable.Rows.Add(0, donorId, canonicalResultSetId);
            }

            return dataTable;
        }
    }
}