using Atlas.ManualTesting.Common.Models.Entities;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.ManualTesting.Common.Repositories
{
    public interface ITestDonorExportRepository
    {
        Task<IEnumerable<TestDonorExportRecord>> GetRecordsWithoutDataRefreshDetails();
        Task<int> AddRecord();
        Task SetExportedDateTimeToNow(int id);
        Task UpdateLatestRecordWithDataRefreshDetails(CompletedDataRefresh dataRefresh);
        Task<int?> GetMaxExportRecordId();
    }

    public class TestDonorExportRepository : ITestDonorExportRepository
    {
        private const string TableName = "TestDonorExportRecords";
        private readonly string connectionString;

        public TestDonorExportRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<TestDonorExportRecord>> GetRecordsWithoutDataRefreshDetails()
        {
            const string sql = $"SELECT * FROM {TableName} WHERE DataRefreshRecordId IS NULL";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QueryAsync<TestDonorExportRecord>(sql, commandTimeout: 180);
            }
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

        public async Task UpdateLatestRecordWithDataRefreshDetails(CompletedDataRefresh dataRefresh)
        {
            var dateTimeNow = DateTimeOffset.UtcNow;

            const string sql = $@"
                UPDATE {TableName} SET
                    DataRefreshCompleted = @{nameof(dateTimeNow)},
                    DataRefreshRecordId = @{nameof(dataRefresh.DataRefreshRecordId)},
                    WasDataRefreshSuccessful = @{nameof(dataRefresh.WasSuccessful)}
                WHERE Id = (SELECT MAX(Id) from {TableName})";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { dateTimeNow, dataRefresh.DataRefreshRecordId, dataRefresh.WasSuccessful });
            }
        }

        public async Task<int?> GetMaxExportRecordId()
        {
            const string sql = $"SELECT MAX(Id) from {TableName}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<int?>(sql)).SingleOrDefault();
            }
        }
    }
}
