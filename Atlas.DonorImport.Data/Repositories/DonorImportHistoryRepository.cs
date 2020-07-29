using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportHistoryRepository
    {
        public Task InsertNewDonorImport(string filename, DateTime uploadTime);
        public Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState state);
    }
    
    public class DonorImportHistoryRepository : IDonorImportHistoryRepository
    {
        private string ConnectionString;

        public DonorImportHistoryRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public async Task InsertNewDonorImport(string filename, DateTime uploadTime)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $@"INSERT INTO DonorImportHistory (Filename, UploadTime, State, LastUpdated) VALUES ((@FileName), (@UploadTime), ${DonorImportState.Started}, GETDATE())";
                connection.Open();
                connection.Execute(sql, new {FileName = filename, UploadTime = uploadTime});
                connection.Close();
            }
        }

        public async Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState state)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $@"UPDATE DonorImportHistory
SET State = (@State), LastUpdated = GETDATE() 
WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                connection.Open();
                connection.Execute(sql, new {FileName = filename, UploadTime = uploadTime, State = state});
                connection.Close();
            }
        }
        
    }
}