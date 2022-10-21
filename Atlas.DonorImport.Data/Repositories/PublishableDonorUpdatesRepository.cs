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
        Task<IEnumerable<PublishableDonorUpdate>> GetOldestDonorUpdates(int batchSize);

        Task DeleteDonorUpdates(IEnumerable<int> updateIds);
    }

    public class PublishableDonorUpdatesRepository : BulkInsertRepository<PublishableDonorUpdate>, IPublishableDonorUpdatesRepository
    {
        public PublishableDonorUpdatesRepository(string connectionString) : base(connectionString, PublishableDonorUpdate.QualifiedTableName)
        {
        }

        public async Task<IEnumerable<PublishableDonorUpdate>> GetOldestDonorUpdates(int batchSize)
        {
            const string sql = $"SELECT TOP (@{nameof(batchSize)}) * FROM {PublishableDonorUpdate.QualifiedTableName} ORDER BY {nameof(PublishableDonorUpdate.Id)}";

            await using(var connection = new SqlConnection(ConnectionString))
            {
                return await connection.QueryAsync<PublishableDonorUpdate>(sql, param: new { batchSize });
            }
        }

        public async Task DeleteDonorUpdates(IEnumerable<int> updateIds)
        {
            const string sql = $"DELETE FROM {PublishableDonorUpdate.QualifiedTableName} WHERE {nameof(PublishableDonorUpdate.Id)} IN @{nameof(updateIds)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, param: new { updateIds });
            }
        }
    }
}
