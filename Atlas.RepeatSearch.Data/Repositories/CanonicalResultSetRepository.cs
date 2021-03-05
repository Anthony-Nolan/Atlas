using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Settings;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.RepeatSearch.Data.Repositories
{
    public interface ICanonicalResultSetRepository
    {
        Task CreateCanonicalResultSet(string searchRequestId, IReadOnlyCollection<int> donorIds);

        /// <returns>
        /// A <see cref="CanonicalResultSet"/> with no search results populated.
        /// </returns>
        Task<CanonicalResultSet> GetCanonicalResultSetSummary(string searchRequestId);
    }

    public class CanonicalResultSetRepository : ICanonicalResultSetRepository
    {
        private readonly string connectionString;

        public CanonicalResultSetRepository(ConnectionStrings connectionStrings)
        {
            connectionString = connectionStrings.RepeatSearchSqlConnectionString;
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

        public async Task<CanonicalResultSet> GetCanonicalResultSetSummary(string searchRequestId)
        {
            var sql = @$"
SELECT * FROM {CanonicalResultSet.QualifiedTableName} 
WHERE {nameof(CanonicalResultSet.OriginalSearchRequestId)} = @{nameof(searchRequestId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<CanonicalResultSet>(sql, new {searchRequestId});
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