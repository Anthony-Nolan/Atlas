using Atlas.Common.Sql.BulkInsert;
using Atlas.ManualTesting.Common.Contexts;
using Atlas.ManualTesting.Common.Models.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.ManualTesting.Common.Repositories
{
    public class MatchProbabilitiesRepository :
        BulkInsertRepository<MatchedDonorProbability>,
        IProcessedResultsRepository<MatchedDonorProbability>
    {
        private const string TableName = nameof(ISearchData<SearchRequestRecord>.MatchProbabilities);
        private readonly string connectionString;

        public MatchProbabilitiesRepository(string connectionString) : base(connectionString, TableName)
        {
            this.connectionString = connectionString;
        }

        public async Task DeleteResults(int searchRequestRecordId)
        {
            const string sql = $@"
                DELETE FROM MatchProbabilities
                FROM MatchProbabilities m
                JOIN MatchedDonors d
                ON m.MatchedDonor_Id = d.Id
                WHERE d.SearchRequestRecord_Id = @{nameof(searchRequestRecordId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchRequestRecordId });
            }
        }
    }
}
