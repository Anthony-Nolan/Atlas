using Atlas.Common.ApplicationInsights;
using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using static Microsoft.Azure.Amqp.Serialization.SerializableType;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IPublishableDonorUpdatesRepository : IBulkInsertRepository<PublishableDonorUpdate>
    {
        Task<IEnumerable<PublishableDonorUpdate>> GetOldestUnpublishedDonorUpdates(int batchSize);
        Task MarkUpdatesAsPublished(IEnumerable<int> updateIds);
        Task DeleteUpdatesPublishedOnOrBefore(DateTimeOffset dateCutOff, int publishedUpdatesToDeleteCap, int publishedUpdatesToDeleteBatchSize);
    }

    public class PublishableDonorUpdatesRepository : BulkInsertRepository<PublishableDonorUpdate>, IPublishableDonorUpdatesRepository
    {
        private readonly ILogger logger;

       public PublishableDonorUpdatesRepository(string connectionString, ILogger logger) : base(connectionString, PublishableDonorUpdate.QualifiedTableName)
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

        public async Task DeleteUpdatesPublishedOnOrBefore(DateTimeOffset dateCutOff, int publishedUpdatesToDeleteCap, int publishedUpdatesToDeleteBatchSize)
        {
            int rowsAffected;
            var totalDeleted = 0;

            var totalRecordsAwaitingDeletion = await CountPublishedUpdatesToDelete(dateCutOff);

            logger.SendTrace($"{totalRecordsAwaitingDeletion} matching rows found in {PublishableDonorUpdate.QualifiedTableName}, cap size {publishedUpdatesToDeleteCap}. Starting batch delete...",
                LogLevel.Info);

            do
            {
                var remainingToDelete = publishedUpdatesToDeleteCap - totalDeleted;
                var effectiveBatchSize = Math.Min(publishedUpdatesToDeleteBatchSize, remainingToDelete);

                const string sql = @$"DELETE TOP (@{nameof(effectiveBatchSize)}) FROM {PublishableDonorUpdate.QualifiedTableName} WHERE
                           {nameof(PublishableDonorUpdate.IsPublished)} = 1 AND
                           {nameof(PublishableDonorUpdate.PublishedOn)} <= @{nameof(dateCutOff)}";

                await using (var connection = new SqlConnection(ConnectionString))
                {
                    rowsAffected = await connection.ExecuteAsync(sql, param: new { dateCutOff, effectiveBatchSize }, commandTimeout: 600);
                    totalDeleted += rowsAffected;
                }
            } while (rowsAffected > 0 && totalDeleted < publishedUpdatesToDeleteCap);

            logger.SendTrace($"Batch delete complete on {PublishableDonorUpdate.QualifiedTableName}. Total rows deleted: {totalDeleted}", LogLevel.Info);
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
