using Atlas.Common.Sql.BulkInsert;
using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.ManualTesting.Common.Repositories
{
    public class LocusMatchDetailsRepository : 
        BulkInsertRepository<LocusMatchDetails>,
        IProcessedResultsRepository<LocusMatchDetails>
    {
        private const string TableName = nameof(ISearchData<SearchRequestRecord>.LocusMatchDetails);
        private readonly string connectionString;

        public LocusMatchDetailsRepository(string connectionString) : base(connectionString, TableName)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteResults(int searchRequestRecordId)
        {
            const string sql = $@"
                DELETE FROM LocusMatchDetails
                FROM LocusMatchDetails c
                JOIN MatchedDonors d
                ON c.MatchedDonor_Id = d.Id
                WHERE d.SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId }, commandTimeout: 600);
            }
        }
    }
}
