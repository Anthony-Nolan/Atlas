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
        public Task<DonorImportState> GetFileStateIfExists(string filename, DateTime uploadTime);
    }
    
    public class DonorImportHistoryRepository : IDonorImportHistoryRepository
    {
        private string ConnectionString;

        public DonorImportHistoryRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task InsertNewDonorImportRecord(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $@"INSERT INTO DonorImportHistory (Filename, UploadTime, FileState, LastUpdated) VALUES ((@FileName), (@UploadTime), (@DonorState), (@Time))";
                connection.Open();
                var state = DonorImportState.Started.ToString();
                connection.Execute(sql, new {FileName = filename, UploadTime = uploadTime, DonorState = state, Time = DateTime.Now});
                connection.Close();
            }
        }

        public async Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState donorState)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $@"UPDATE DonorImportHistory
SET FileState = (@State), LastUpdated = (@Time)
WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                connection.Open();
                var state = donorState.ToString();
                await connection.ExecuteAsync(sql, new {FileName = filename, UploadTime = uploadTime, State = state, Time = DateTime.Now});
                connection.Close();
            }
        }

        public async Task<DonorImportState> GetFileStateIfExists(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                const string sql = "SELECT FileState FROM DonorImportHistory WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                connection.Open();
                var results = connection.Query<DonorImportState>(sql, new {FileName = filename, UploadTime = uploadTime}).ToArray();
                return !results.Any() ? DonorImportState.NotFound : results.Single();
            }
        }
    }
}