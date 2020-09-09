using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportHistoryRepository
    {
        public Task InsertNewDonorImportRecord(string filename, DateTime uploadTime);
        public Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState donorState);
        public Task UpdateDonorImportBatchCount(string filename, DateTime uploadTime);
        public Task<DonorImportHistoryRecord> GetFileIfExists(string filename, DateTime uploadTime);
        public Task<IReadOnlyCollection<DonorImportHistoryRecord>> GetLongRunningFiles(TimeSpan duration);
    }

    public class DonorImportHistoryRepository : IDonorImportHistoryRepository
    {
        private readonly string connectionString;

        public DonorImportHistoryRepository(string connectionString)
        {
            this.connectionString = connectionString;
            // Without setting up this type map 
            SqlMapper.AddTypeMap(typeof(DateTime), System.Data.DbType.DateTime2);
        }

        public async Task InsertNewDonorImportRecord(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                var sql =
                    $@"INSERT INTO DonorImportHistory (Filename, UploadTime, FileState, LastUpdated, ImportBegin) VALUES ((@FileName), (@UploadTime), (@DonorState), (@Time), GETUTCDATE())";
                await connection.ExecuteAsync(sql,
                    new {FileName = filename, UploadTime = uploadTime, DonorState = DonorImportState.Started.ToString(), Time = DateTime.UtcNow});
            }
        }

        public async Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState donorState)
        {
            var updateImportEndSql = $", ImportEnd = GETUTCDATE()";
            var incrementFailureCountSql = $", FailureCount = FailureCount + 1";

            var additionalUpdateClause = donorState switch
            {
                DonorImportState.Started => "",
                DonorImportState.Completed => updateImportEndSql,
                DonorImportState.FailedPermanent => incrementFailureCountSql,
                DonorImportState.FailedUnexpectedly => incrementFailureCountSql,
                DonorImportState.Stalled => "",
                _ => throw new ArgumentOutOfRangeException()
            };
            
            await using (var connection = new SqlConnection(connectionString))
            {
                var sql = $@"UPDATE DonorImportHistory
SET FileState = (@State), LastUpdated = (@Time) 
{additionalUpdateClause}
WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                await connection.ExecuteAsync(sql,
                    new {FileName = filename, UploadTime = uploadTime, State = donorState.ToString(), Time = DateTime.UtcNow});
            }
        }

        public async Task UpdateDonorImportBatchCount(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                var sql = $@"UPDATE DonorImportHistory
SET ImportedBatchCount = ImportedBatchCount + 1
WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                await connection.ExecuteAsync(sql, new {FileName = filename, UploadTime = uploadTime});
            }
        }

        public async Task<DonorImportHistoryRecord> GetFileIfExists(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                const string sql = "SELECT * FROM DonorImportHistory WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                var results = connection.Query<DonorImportHistoryRecord>(sql, new {FileName = filename, UploadTime = uploadTime}).ToArray();
                return results.SingleOrDefault();
            }
        }

        public async Task<IReadOnlyCollection<DonorImportHistoryRecord>> GetLongRunningFiles(TimeSpan duration)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                var earliestTime = DateTime.UtcNow.Subtract(duration);
                const string sql = "SELECT * FROM DonorImportHistory WHERE FileState = (@fileState) AND ImportBegin < (@timeToCheck)";
                return connection.Query<DonorImportHistoryRecord>(sql, new {fileState = DonorImportState.Started.ToString(), timeToCheck = earliestTime}).ToArray();
            }
        }
    }
}