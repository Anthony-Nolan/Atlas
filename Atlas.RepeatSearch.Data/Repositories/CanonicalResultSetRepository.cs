using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Atlas.RepeatSearch.Data.Models;
using Atlas.RepeatSearch.Data.Settings;
using Dapper;
using Microsoft.Data.SqlClient;
using MoreLinq;

namespace Atlas.RepeatSearch.Data.Repositories
{
    /// <summary>
    /// The right of one delivery of a results message to store the canonical result set of a search.
    /// </summary>
    /// <param name="SetId">Id of the canonical result set.</param>
    /// <param name="StoreAttemptId">Id of this delivery's attempt: the set must still have it to take chunks and to be completed.</param>
    /// <param name="TookOver">True if the set was already in progress, from an earlier delivery, so its rows were deleted.</param>
    public record CanonicalResultSetStoreClaim(int SetId, Guid StoreAttemptId, bool TookOver);

    public interface ICanonicalResultSetRepository
    {
        /// <summary>
        /// Starts storing the canonical result set of an original search.
        /// Creates the set as <see cref="CanonicalResultSetStatus.InProgress"/>, or takes over a set that is still in progress from an
        /// earlier delivery and deletes its rows.
        /// </summary>
        /// <returns>The claim to add chunks with, or null if the set is already complete.</returns>
        Task<CanonicalResultSetStoreClaim> StartStoringResultSet(string searchRequestId);

        /// <summary>
        /// Inserts one chunk of donor codes, committed on its own.
        /// Throws <see cref="CanonicalResultSetClaimLostException"/> if another delivery has taken over the set.
        /// </summary>
        Task AddResultsChunk(CanonicalResultSetStoreClaim claim, IReadOnlyCollection<string> externalDonorCodes);

        /// <summary>
        /// Marks the set as <see cref="CanonicalResultSetStatus.Complete"/>.
        /// Throws <see cref="CanonicalResultSetClaimLostException"/> if another delivery has taken over the set.
        /// </summary>
        Task CompleteResultSet(CanonicalResultSetStoreClaim claim);

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
        private const int DeleteBatchSize = 10000;

        private readonly string connectionString;
        private readonly int sqlBulkCopyBatchSize;
        private readonly int sqlBulkCopyBatchTimeout;

        public CanonicalResultSetRepository(ConnectionStrings connectionStrings, StoreOriginalSearchResultsBulkCopySettings settings)
        {
            connectionString = connectionStrings.RepeatSearchSqlConnectionString;
            sqlBulkCopyBatchSize = settings.BatchSize;
            sqlBulkCopyBatchTimeout = settings.Timeout;
        }

        public async Task<CanonicalResultSetStoreClaim> StartStoringResultSet(string searchRequestId)
        {
            var storeAttemptId = Guid.NewGuid();

            // The insert is tried first, without a check: the set rarely exists, and a check before the insert would need range locks
            // (updlock, serializable) to be safe. On a duplicate key, the set is taken over if it is still in progress.
            var sql = @$"
                BEGIN TRY
                    INSERT INTO {CanonicalResultSet.QualifiedTableName}
                    ({nameof(CanonicalResultSet.OriginalSearchRequestId)}, {nameof(CanonicalResultSet.Status)}, {nameof(CanonicalResultSet.StoreAttemptId)})
                    VALUES (@{nameof(searchRequestId)}, @InProgress, @{nameof(storeAttemptId)});

                    SELECT CAST(SCOPE_IDENTITY() AS int) AS SetId, CAST(0 AS bit) AS TookOver;
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() NOT IN (2601, 2627)
                        THROW;

                    UPDATE {CanonicalResultSet.QualifiedTableName}
                    SET {nameof(CanonicalResultSet.StoreAttemptId)} = @{nameof(storeAttemptId)}
                    OUTPUT inserted.{nameof(CanonicalResultSet.Id)} AS SetId, CAST(1 AS bit) AS TookOver
                    WHERE {nameof(CanonicalResultSet.OriginalSearchRequestId)} = @{nameof(searchRequestId)}
                    AND {nameof(CanonicalResultSet.Status)} = @InProgress;
                END CATCH";

            StartedSet started;
            await using (var conn = new SqlConnection(connectionString))
            {
                started = await conn.QuerySingleOrDefaultAsync<StartedSet>(sql, new
                {
                    searchRequestId,
                    storeAttemptId,
                    InProgress = (int)CanonicalResultSetStatus.InProgress
                });
            }

            // No row: the set exists and is complete.
            if (started == null)
            {
                return null;
            }

            if (started.TookOver)
            {
                await DeleteAllResultsFromSet(started.SetId);
            }

            return new CanonicalResultSetStoreClaim(started.SetId, storeAttemptId, started.TookOver);
        }

        public async Task AddResultsChunk(CanonicalResultSetStoreClaim claim, IReadOnlyCollection<string> externalDonorCodes)
        {
            // Not atomic with the insert: see CanonicalResultSet.StoreAttemptId for why the unique index of SearchResults keeps the set correct.
            await ThrowIfClaimLost(claim);

            if (externalDonorCodes.Count == 0)
            {
                return;
            }

            await AddResultsToSet(externalDonorCodes, claim.SetId, SqlBulkCopyOptions.UseInternalTransaction);
        }

        public async Task CompleteResultSet(CanonicalResultSetStoreClaim claim)
        {
            var sql = @$"
                UPDATE {CanonicalResultSet.QualifiedTableName}
                SET {nameof(CanonicalResultSet.Status)} = @Complete
                WHERE {nameof(CanonicalResultSet.Id)} = @{nameof(claim.SetId)}
                AND {nameof(CanonicalResultSet.StoreAttemptId)} = @{nameof(claim.StoreAttemptId)}
                AND {nameof(CanonicalResultSet.Status)} = @InProgress";

            int updatedRows;
            await using (var conn = new SqlConnection(connectionString))
            {
                updatedRows = await conn.ExecuteAsync(sql, new
                {
                    claim.SetId,
                    claim.StoreAttemptId,
                    InProgress = (int)CanonicalResultSetStatus.InProgress,
                    Complete = (int)CanonicalResultSetStatus.Complete
                });
            }

            if (updatedRows == 0)
            {
                throw new CanonicalResultSetClaimLostException(claim);
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

        private async Task AddResultsToSet(
            IReadOnlyCollection<string> externalDonorCodes,
            int resultSetId,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default)
        {
            var resultEntryDataTable = BuildResultsInsertDataTable(resultSetId, externalDonorCodes);
            using (var sqlBulk = BuildResultsBulkCopy(options))
            {
                await sqlBulk.WriteToServerAsync(resultEntryDataTable);
            }
        }

        private async Task ThrowIfClaimLost(CanonicalResultSetStoreClaim claim)
        {
            var sql = @$"
                SELECT COUNT(*) FROM {CanonicalResultSet.QualifiedTableName}
                WHERE {nameof(CanonicalResultSet.Id)} = @{nameof(claim.SetId)}
                AND {nameof(CanonicalResultSet.StoreAttemptId)} = @{nameof(claim.StoreAttemptId)}
                AND {nameof(CanonicalResultSet.Status)} = @InProgress";

            int claimedSets;
            await using (var conn = new SqlConnection(connectionString))
            {
                claimedSets = await conn.ExecuteScalarAsync<int>(sql, new
                {
                    claim.SetId,
                    claim.StoreAttemptId,
                    InProgress = (int)CanonicalResultSetStatus.InProgress
                });
            }

            if (claimedSets == 0)
            {
                throw new CanonicalResultSetClaimLostException(claim);
            }
        }

        /// <summary>
        /// Deletes in batches, so that a large set does not fill the transaction log in one statement.
        /// </summary>
        private async Task DeleteAllResultsFromSet(int resultSetId)
        {
            var sql = @$"
                DELETE TOP ({DeleteBatchSize}) FROM {SearchResult.QualifiedTableName}
                WHERE {nameof(SearchResult.CanonicalResultSetId)} = @{nameof(resultSetId)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                int deletedRows;
                do
                {
                    deletedRows = await conn.ExecuteAsync(sql, new {resultSetId}, commandTimeout: sqlBulkCopyBatchTimeout);
                } while (deletedRows == DeleteBatchSize);
            }
        }

        private SqlBulkCopy BuildResultsBulkCopy(SqlBulkCopyOptions options)
        {
            var sqlBulk = new SqlBulkCopy(connectionString, options) {BatchSize = sqlBulkCopyBatchSize, DestinationTableName = SearchResult.QualifiedTableName};
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

        private class StartedSet
        {
            public int SetId { get; set; }
            public bool TookOver { get; set; }
        }
    }

    public class CanonicalResultSetClaimLostException : Exception
    {
        public CanonicalResultSetClaimLostException(CanonicalResultSetStoreClaim claim)
            : base($"Another delivery has taken over or completed canonical result set {claim.SetId}, so store attempt {claim.StoreAttemptId} stops.")
        {
        }
    }
}
