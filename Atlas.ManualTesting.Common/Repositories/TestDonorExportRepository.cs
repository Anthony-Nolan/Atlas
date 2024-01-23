using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.ManualTesting.Common.Repositories
{
    public interface ITestDonorExportRepository
    {
        Task<int> AddRecord();
        Task SetExportedDateTimeToNow(int id);
        Task SetDataRefreshRecordId(int exportRecordId, int dataRefreshRecordId);
        Task SetCompletedDataRefreshInfo(CompletedDataRefresh dataRefresh);
        Task<TestDonorExportRecord?> GetLastExportRecord();
    }

    public class TestDonorExportRepository : ITestDonorExportRepository
    {
        private const string TableName = "TestDonorExportRecords";
        private readonly string connectionString;

        public TestDonorExportRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> AddRecord()
        {
            var dateTimeNow = DateTime.UtcNow;
            const string sql = $@"INSERT INTO {TableName} (Started) VALUES (@{nameof(dateTimeNow)}); 
                                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<int>(sql, new { dateTimeNow }, commandTimeout: 180)).Single();
            }
        }

        public async Task SetExportedDateTimeToNow(int id)
        {
            var dateTimeNow = DateTimeOffset.UtcNow;
            const string sql = $@"UPDATE dbo.TestDonorExportRecords SET Exported = @{nameof(dateTimeNow)} WHERE Id = @{nameof(id)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { dateTimeNow, id });
            }
        }

        public async Task SetDataRefreshRecordId(int exportId, int dataRefreshRecordId)
        {
            var dateTimeNow = DateTimeOffset.UtcNow;
            const string sql = $@"UPDATE dbo.TestDonorExportRecords 
                                SET DataRefreshRecordId = @{nameof(dataRefreshRecordId)}
                                WHERE Id = @{nameof(exportId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { dateTimeNow, exportId, dataRefreshRecordId });
            }
        }

        public async Task SetCompletedDataRefreshInfo(CompletedDataRefresh dataRefresh)
        {
            var dateTimeNow = DateTimeOffset.UtcNow;

            const string sql = $@"
                UPDATE {TableName} SET
                    DataRefreshCompleted = @{nameof(dateTimeNow)},
                    WasDataRefreshSuccessful = @{nameof(dataRefresh.WasSuccessful)}
                WHERE DataRefreshRecordId = @{nameof(dataRefresh.DataRefreshRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { dateTimeNow, dataRefresh.DataRefreshRecordId, dataRefresh.WasSuccessful });
            }
        }

        public async Task<TestDonorExportRecord?> GetLastExportRecord()
        {
            const string sql = $"SELECT TOP 1 * FROM {TableName} ORDER BY Id DESC";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<TestDonorExportRecord>(sql)).SingleOrDefault();
            }
        }
    }
}
