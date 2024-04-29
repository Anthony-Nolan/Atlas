using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IHomeworkSetRepository
    {
        Task<int> Add(string setName, string resultsPath, string matchLoci, string hlaNomenclatureVersion);
        Task<HomeworkSet> Get(int setId);
    }

    public class HomeworkSetRepository : IHomeworkSetRepository
    {
        private readonly string connectionString;

        public HomeworkSetRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> Add(string setName, string resultsPath, string matchLoci, string hlaNomenclatureVersion)
        {
            const string sql = $@"
                INSERT INTO HomeworkSets(
                    {nameof(HomeworkSet.SetName)},
                    {nameof(HomeworkSet.ResultsPath)},
                    {nameof(HomeworkSet.MatchLoci)},
                    {nameof(HomeworkSet.HlaNomenclatureVersion)}
                ) VALUES(
                    @{nameof(setName)},
                    @{nameof(resultsPath)},
                    @{nameof(matchLoci)},
                    @{nameof(hlaNomenclatureVersion)}
                );
                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using (var connection = new SqlConnection(connectionString))
            {
                return (await connection.QueryAsync<int>(sql, new { setName, resultsPath, matchLoci, hlaNomenclatureVersion })).Single();
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