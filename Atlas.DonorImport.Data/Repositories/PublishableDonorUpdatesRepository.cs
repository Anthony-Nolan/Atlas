using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IPublishableDonorUpdatesRepository : IBulkInsertRepository<PublishableDonorUpdate>
    {
        Task<IEnumerable<PublishableDonorUpdate>> GetOldestUnpublishedDonorUpdates(int batchSize);
        Task MarkUpdatesAsPublished(IEnumerable<int> updateIds);
        Task DeleteUpdatesPublishedOnOrBefore(DateTimeOffset dateCutOff);
    }

    public class PublishableDonorUpdatesRepository : BulkInsertRepository<PublishableDonorUpdate>, IPublishableDonorUpdatesRepository
    {
        public PublishableDonorUpdatesRepository(string connectionString) : base(connectionString, PublishableDonorUpdate.QualifiedTableName)
        {
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

        public async Task DeleteUpdatesPublishedOnOrBefore(DateTimeOffset dateCutOff)
        {
            const string sql = @$"DELETE FROM {PublishableDonorUpdate.QualifiedTableName} WHERE
                               {nameof(PublishableDonorUpdate.IsPublished)} = 1 AND
                               {nameof(PublishableDonorUpdate.PublishedOn)} <= @{nameof(dateCutOff)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, param: new { dateCutOff });
            }
        }
    }
}
