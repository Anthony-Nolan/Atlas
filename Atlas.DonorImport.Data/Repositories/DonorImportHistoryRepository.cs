using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportHistoryRepository
    {
        public Task InsertNewDonorImportRecord(string filename, DateTime uploadTime);
        public Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState state);
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
                connection.Execute(sql, new {FileName = filename, UploadTime = uploadTime, DonorState = DonorImportState.Started, Time = DateTime.Now});
                connection.Close();
            }
        }

        public async Task UpdateDonorImportState(string filename, DateTime uploadTime, DonorImportState state)
        {
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var sql = $@"UPDATE DonorImportHistory
SET FileState = (@State), LastUpdated = (@Time)
WHERE Filename = (@Filename) AND UploadTime = (@UploadTime)";
                connection.Open();
                connection.Execute(sql, new {FileName = filename, UploadTime = uploadTime, State = state, Time = DateTime.Now});
                connection.Close();
            }
        }
        
    }
}