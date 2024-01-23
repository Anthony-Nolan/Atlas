using Atlas.Common.Sql.BulkInsert;
using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.ManualTesting.Common.Repositories
{
    public interface IMatchedDonorsRepository
    {
        Task<int?> GetMatchedDonorId(int searchRequestRecordId, string donorCode);
    }

    public class MatchedDonorsRepository : 
        BulkInsertRepository<MatchedDonor>, 
        IMatchedDonorsRepository, 
        IProcessedResultsRepository<MatchedDonor>
    {
        private const string TableName = nameof(ISearchData<SearchRequestRecord>.MatchedDonors);
        private readonly string connectionString;

        public MatchedDonorsRepository(string connectionString) : base(connectionString, TableName)
        {
            this.connectionString = connectionString;
        }

        public async Task<int?> GetMatchedDonorId(int searchRequestRecordId, string donorCode)
        {
            const string sql = @$"SELECT Id FROM MatchedDonors WHERE 
                SearchRequestRecord_Id = @{nameof(searchRequestRecordId)} AND
                DonorCode = @{nameof(donorCode)}";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<int>(sql, new { searchRequestRecordId, donorCode })).SingleOrDefault();
            }
        }

        public async Task DeleteResults(int searchRequestRecordId)
        {
            const string sql = $@"DELETE FROM MatchedDonors WHERE SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }
    }
}
