using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Test.Integration.TestHelpers
{
    /// <summary>
    /// Contains methods for inspecting data that are only necessary in the context of integration tests.
    /// </summary>
    public interface IPublishableDonorUpdatesInspectionRepository
    {
        Task<int> Count();

        Task<IEnumerable<PublishableDonorUpdate>> Get(IEnumerable<int> donorIds, bool isAvailableForSearch);

        Task<IEnumerable<PublishableDonorUpdate>> GetAll();

        Task RemoveAll();
    }

    public class PublishableDonorUpdatesInspectionRepository : IPublishableDonorUpdatesInspectionRepository
    {
        private readonly string connectionString;

        public PublishableDonorUpdatesInspectionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> Count()
        {
            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(*) FROM {PublishableDonorUpdate.QualifiedTableName}");
            }
        }

        public async Task<IEnumerable<PublishableDonorUpdate>> Get(IEnumerable<int> donorIds, bool isAvailableForSearch)
        {
            const string sql = $@"
                SELECT * FROM {PublishableDonorUpdate.QualifiedTableName} WHERE
                {nameof(PublishableDonorUpdate.DonorId)} IN @{nameof(donorIds)} AND
                JSON_VALUE({nameof(PublishableDonorUpdate.SearchableDonorUpdate)}, '$.{nameof(SearchableDonorUpdate.IsAvailableForSearch)}') = @{nameof(isAvailableForSearch)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<PublishableDonorUpdate>(sql, param: new { donorIds, isAvailableForSearch });
            }
        }

        public async Task<IEnumerable<PublishableDonorUpdate>> GetAll()
        {
            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<PublishableDonorUpdate>($"SELECT * FROM {PublishableDonorUpdate.QualifiedTableName}");
            }
        }

        public async Task RemoveAll()
        {
            const string sql = @$"DELETE FROM {PublishableDonorUpdate.QualifiedTableName}";

            await using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync(sql);
        }
    }
}