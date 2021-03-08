using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Settings;
using Dapper;
using Microsoft.Data.SqlClient;
using MoreLinq;

namespace Atlas.RepeatSearch.Data.Repositories
{
    public interface ICanonicalResultSetRepository
    {
        Task CreateCanonicalResultSet(string searchRequestId, IReadOnlyCollection<string> externalDonorCodes);

        /// <returns>
        /// A <see cref="CanonicalResultSet"/> with no search results populated.
        /// </returns>
        Task<CanonicalResultSet> GetCanonicalResultSetSummary(string searchRequestId);

        Task<ICollection<SearchResult>> GetCanonicalResults(string searchRequestId);

        Task RemoveResultsFromSet(string searchRequestId, IReadOnlyCollection<string> donorCodesToRemove);

        Task AddResultsToSet(string searchRequestId, IReadOnlyCollection<string> donorCodesToAdd);
    }

    public class CanonicalResultSetRepository : ICanonicalResultSetRepository
    {
        private readonly string connectionString;

        public CanonicalResultSetRepository(ConnectionStrings connectionStrings)
        {
            connectionString = connectionStrings.RepeatSearchSqlConnectionString;
        }

        public async Task CreateCanonicalResultSet(string searchRequestId, IReadOnlyCollection<string> externalDonorCodes)
        {
            using (var transactionScope = new AsyncTransactionScope())
            {
                var resultSetId = await CreateResultSetEntity(searchRequestId);
                await AddResultsToSet(externalDonorCodes, resultSetId);
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

        public async Task<ICollection<SearchResult>> GetCanonicalResults(string searchRequestId)
        {
            var sql = @$"
SELECT * FROM {SearchResult.QualifiedTableName} sr
JOIN {CanonicalResultSet.QualifiedTableName} crs on crs.{nameof(CanonicalResultSet.Id)} = sr.{nameof(SearchResult.CanonicalResultSetId)} 
WHERE crs.{nameof(CanonicalResultSet.OriginalSearchRequestId)} = @{nameof(searchRequestId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<SearchResult>(sql, new {searchRequestId})).ToList();
            }
        }

        public async Task RemoveResultsFromSet(string searchRequestId, IReadOnlyCollection<string> donorCodesToRemove)
        {
            var sql = @$"
DELETE sr FROM {SearchResult.QualifiedTableName} sr
JOIN {CanonicalResultSet.QualifiedTableName} crs on crs.{nameof(CanonicalResultSet.Id)} = sr.{nameof(SearchResult.CanonicalResultSetId)} 
WHERE crs.{nameof(CanonicalResultSet.OriginalSearchRequestId)} = @{nameof(searchRequestId)} 
AND sr.{nameof(SearchResult.ExternalDonorCode)} IN @Ids
                ";

            await using (var connection = new SqlConnection(connectionString))
            {
                foreach (var donorCodeBatch in donorCodesToRemove.Batch(2000))
                {
                    await connection.ExecuteAsync(sql, new {searchRequestId, Ids = donorCodeBatch.ToList()});
                }
            }
        }

        public async Task AddResultsToSet(string searchRequestId, IReadOnlyCollection<string> donorCodesToAdd)
        {
            var setId = (await GetCanonicalResultSetSummary(searchRequestId)).Id;
            await AddResultsToSet(donorCodesToAdd, setId);
        }

        private async Task AddResultsToSet(IReadOnlyCollection<string> externalDonorCodes, int resultSetId)
        {
            var resultEntryDataTable = BuildResultsInsertDataTable(resultSetId, externalDonorCodes);
            using (var sqlBulk = BuildResultsBulkCopy())
            {
                await sqlBulk.WriteToServerAsync(resultEntryDataTable);
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
            sqlBulk.ColumnMappings.Add(nameof(SearchResult.ExternalDonorCode), nameof(SearchResult.ExternalDonorCode));
            sqlBulk.ColumnMappings.Add(nameof(SearchResult.CanonicalResultSetId), nameof(SearchResult.CanonicalResultSetId));
            return sqlBulk;
        }

        private DataTable BuildResultsInsertDataTable(int canonicalResultSetId, IEnumerable<string> externalDonorCodes)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(nameof(SearchResult.Id));
            dataTable.Columns.Add(nameof(SearchResult.ExternalDonorCode));
            dataTable.Columns.Add(nameof(SearchResult.CanonicalResultSetId));

            foreach (var donorId in externalDonorCodes)
            {
                dataTable.Rows.Add(0, donorId, canonicalResultSetId);
            }

            return dataTable;
        }
    }
}