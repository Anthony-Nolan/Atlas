using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IHomeworkSetRepository
    {
        Task<int> Add(string setName, string resultsPath, string matchLoci);
        Task<HomeworkSet> Get(int setId);
    }

    public class HomeworkSetRepository : IHomeworkSetRepository
    {
        private readonly string connectionString;

        public HomeworkSetRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> Add(string setName, string resultsPath, string matchLoci)
        {
            const string sql = $@"
                INSERT INTO HomeworkSets(
                    {nameof(HomeworkSet.SetName)},
                    {nameof(HomeworkSet.ResultsPath)},
                    {nameof(HomeworkSet.MatchLoci)}
                ) VALUES(
                    @{nameof(setName)},
                    @{nameof(resultsPath)},
                    @{nameof(matchLoci)}
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using (var connection = new SqlConnection(connectionString))
            {
                return (await connection.QueryAsync<int>(sql, new { setName, resultsPath, matchLoci })).Single();
            }
        }

        public async Task<HomeworkSet> Get(int setId)
        {
            const string sql = $@" SELECT * FROM HomeworkSets WHERE Id = @{nameof(setId)}";

            await using (var connection = new SqlConnection(connectionString))
            {
                return connection.QuerySingleOrDefault<HomeworkSet>(sql, new { setId });
            }
        }
    }
}