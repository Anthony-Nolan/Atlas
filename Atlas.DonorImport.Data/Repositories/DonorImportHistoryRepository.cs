using System;
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
        public Task<DonorImportState?> GetFileStateIfExists(string filename, DateTime uploadTime);
    }

    public class DonorImportHistoryRepository : IDonorImportHistoryRepository
    {
        private readonly string connectionString;

        public DonorImportHistoryRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task InsertNewDonorImportRecord(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                var sql =
                    $@"INSERT INTO DonorImportHistory (Filename, UploadTime, FileState, LastUpdated) VALUES ((@FileName), (@UploadTime), (@DonorState), (@Time))";
                await connection.ExecuteAsync(sql,
                    new {FileName = filename, UploadTime = uploadTime, DonorState = DonorImportState.Started.ToString(), Time = DateTime.UtcNow});
            }
        }

        public async Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState donorState)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                var sql = $@"UPDATE DonorImportHistory
SET FileState = (@State), LastUpdated = (@Time)
WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                await connection.ExecuteAsync(sql,
                    new {FileName = filename, UploadTime = uploadTime, State = donorState.ToString(), Time = DateTime.UtcNow});
            }
        }

        public async Task<DonorImportState?> GetFileStateIfExists(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(connectionString))
            {
                const string sql = "SELECT FileState FROM DonorImportHistory WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                var results = connection.Query<DonorImportState?>(sql, new {FileName = filename, UploadTime = uploadTime}).ToArray();
                return results.SingleOrDefault();
            }
        }
    }
}