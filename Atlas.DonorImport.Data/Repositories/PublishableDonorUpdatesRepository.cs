using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IPublishableDonorUpdatesRepository : IBulkInsertRepository<PublishableDonorUpdate>
    {
        Task<IEnumerable<PublishableDonorUpdate>> GetOldestUnpublishedDonorUpdates(int batchSize);
        Task MarkUpdatesAsPublished(IEnumerable<int> updateIds);
        Task DeleteUpdatesPublishedOnOrBefore(DateTimeOffset dateCutOff, int publishedDonorsToDeleteCap, int publishedDonorsToDeleteBatchSize);
    }

    public class PublishableDonorUpdatesRepository : BulkInsertRepository<PublishableDonorUpdate>, IPublishableDonorUpdatesRepository
    {
        private readonly ILogger<PublishableDonorUpdatesRepository> logger;

       public PublishableDonorUpdatesRepository(string connectionString, ILogger<PublishableDonorUpdatesRepository> logger) : base(connectionString, PublishableDonorUpdate.QualifiedTableName)
       {
           this.logger = logger;
       }

        public async Task<IEnumerable<PublishableDonorUpdate>> GetOldestUnpublishedDonorUpdates(int batchSize)
        {
            const string sql = @$"SELECT TOP (@{nameof(batchSize)}) * FROM {PublishableDonorUpdate.QualifiedTableName}
                               WHERE {nameof(PublishableDonorUpdate.IsPublished)} = 0
                               ORDER BY {nameof(PublishableDonorUpdate.Id)}";

            await using(var connection = new SqlConnection(ConnectionString))
            {
                return await connection.QueryAsync<PublishableDonorUpdate>(sql, param: new { batchSize });
            }
        }

        public async Task MarkUpdatesAsPublished(IEnumerable<int> updateIds)
        {
            var dateTimeNow = DateTimeOffset.Now;
            const string sql = @$"UPDATE {PublishableDonorUpdate.QualifiedTableName} SET
                               {nameof(PublishableDonorUpdate.IsPublished)} = 1,
                               {nameof(PublishableDonorUpdate.PublishedOn)} = @{nameof(dateTimeNow)}
                               WHERE {nameof(PublishableDonorUpdate.Id)} IN @{nameof(updateIds)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, param: new { dateTimeNow, updateIds });
            }
        }

        public async Task DeleteUpdatesPublishedOnOrBefore(DateTimeOffset dateCutOff, int publishedDonorsToDeleteCap, int publishedDonorsToDeleteBatchSize)
        {
            int rowsAffected;
            var totalDeleted = 0;

            var totalRecordsAwaitingDeletion = await CountPublishedUpdatesToDelete(dateCutOff);

            logger.LogInformation("{MatchingRows} matching rows found in {Table}. Starting batch delete...", totalRecordsAwaitingDeletion, PublishableDonorUpdate.QualifiedTableName);

            const string sql = @$"DELETE TOP (@{nameof(publishedDonorsToDeleteBatchSize)}) FROM {PublishableDonorUpdate.QualifiedTableName} WHERE
                               {nameof(PublishableDonorUpdate.IsPublished)} = 1 AND
                               {nameof(PublishableDonorUpdate.PublishedOn)} <= @{nameof(dateCutOff)}";

            do
            {
                await using (var connection = new SqlConnection(ConnectionString))
                {
                    rowsAffected = await connection.ExecuteAsync(sql, param: new { dateCutOff, publishedDonorsToDeleteBatchSize }, commandTimeout: 600);
                    totalDeleted += rowsAffected;
                }
            } while (rowsAffected > 0 && totalDeleted < publishedDonorsToDeleteCap);

            logger.LogInformation("Batch delete complete on {Table}. Total rows deleted: {TotalDeleted}",
                PublishableDonorUpdate.QualifiedTableName, totalDeleted);
        }

        private async Task<int> CountPublishedUpdatesToDelete(DateTimeOffset dateCutoff)
        {
            const string sql = @$"SELECT COUNT(*) FROM {PublishableDonorUpdate.QualifiedTableName} WHERE
                               {nameof(PublishableDonorUpdate.IsPublished)} = 1 AND
                               {nameof(PublishableDonorUpdate.PublishedOn)} <= @{nameof(dateCutoff)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                return await connection.ExecuteScalarAsync<int>(sql, param: new { dateCutoff }, commandTimeout: 600);
            }
        }
    }
}
