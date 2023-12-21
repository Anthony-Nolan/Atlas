using Microsoft.Data.SqlClient;
using Atlas.MatchPrediction.Test.Validation.Data.Models;
using Dapper;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories
{
    public interface ISearchSetRepository
    {
        Task<int> Add(int testDonorExportId, string donorType, int mismatchCount, string matchLoci);
        Task MarkSearchSetAsComplete(int searchSetId);
    }

    public class SearchSetRepository : ISearchSetRepository
    {
        private readonly string connectionString;

        public SearchSetRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> Add(int testDonorExportId, string donorType, int mismatchCount, string matchLoci)
        {
            const string sql = $@"
                INSERT INTO SearchSets(
                    {nameof(SearchSet.TestDonorExportRecord_Id)},
                    {nameof(SearchSet.DonorType)},
                    {nameof(SearchSet.MismatchCount)},
                    {nameof(SearchSet.MatchLoci)}
                ) VALUES(
                    @{nameof(testDonorExportId)},
                    @{nameof(donorType)},
                    @{nameof(mismatchCount)},
                    @{nameof(matchLoci)}
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using (var connection = new SqlConnection(connectionString))
            {
                return (await connection.QueryAsync<int>(sql, new { testDonorExportId, donorType, mismatchCount, matchLoci })).Single();
            }
        }

        public async Task MarkSearchSetAsComplete(int searchSetId)
        {
            const string sql = $@"
                UPDATE SearchSets
                SET
                    {nameof(SearchSet.SearchRequestsSubmitted)} = 1
                WHERE
                    {nameof(SearchSet.Id)} = @{nameof(searchSetId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(sql, new { searchSetId });
            }
        }
    }
}