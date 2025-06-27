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
        private readonly int sqlBulkCopyBatchSize;
        private readonly int sqlBulkCopyBatchTimeout;

        public CanonicalResultSetRepository(ConnectionStrings connectionStrings, StoreOriginalSearchResultsBulkCopySettings settings)
        {
            connectionString = connectionStrings.RepeatSearchSqlConnectionString;
            sqlBulkCopyBatchSize = settings.BatchSize;
            sqlBulkCopyBatchTimeout = settings.Timeout;
        }

        public async Task CreateCanonicalResultSet(string searchRequestId, IReadOnlyCollection<string> externalDonorCodes)
        {
            using (var transactionScope = new AsyncTransactionScope())
            {
                var resultSetId = await CreateResultSetIfNotExists(searchRequestId);
                if (resultSetId is null) // null is returned - results are already saved
                    return;

                await AddResultsToSet(externalDonorCodes, resultSetId.Value);
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

        /// <returns>The id of created result set entity or null if it already exists</returns>
        private async Task<int?> CreateResultSetIfNotExists(string searchRequestId)
        {
            // Don't use early return approach because it won't work without query hints: record may appear in database
            // after we checked if it exists, but before we insert 'our' record.
            // With hints (updlock, serializable), it will use update lock and range locks which may prevent 
            // from inserting results from other searches until we commit current transaction (i.e. when we insert all donor ids)
            // With all above and the fact that existing record for search request id is rare case, we're tring to insert the record first. 
            // Then in case of exception, we're checking if records exists and return null idicating that new record wasn't created.
            var sql = @$"
                BEGIN TRY
                    INSERT INTO {CanonicalResultSet.QualifiedTableName}
                    ({nameof(CanonicalResultSet.OriginalSearchRequestId)})
                    VALUES(@{nameof(searchRequestId)});

                    SELECT CAST(SCOPE_IDENTITY() as int);
                END TRY
                BEGIN CATCH 
                    IF EXISTS (SELECT id FROM RepeatSearch.CanonicalResultSets WHERE OriginalSearchRequestId = @searchRequestId)
                        SELECT null
                    ELSE
                    THROW
                END CATCH
                ";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleAsync<int?>(sql, new {searchRequestId});
            }
        }

        private SqlBulkCopy BuildResultsBulkCopy()
        {
            var sqlBulk = new SqlBulkCopy(connectionString) {BatchSize = sqlBulkCopyBatchSize, DestinationTableName = SearchResult.QualifiedTableName};
            sqlBulk.BulkCopyTimeout = sqlBulkCopyBatchTimeout;
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