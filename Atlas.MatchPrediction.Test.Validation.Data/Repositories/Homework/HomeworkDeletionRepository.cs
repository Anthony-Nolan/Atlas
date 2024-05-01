using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IHomeworkDeletionRepository
    {
        Task DeleteAll();
    }

    public class HomeworkDeletionRepository : IHomeworkDeletionRepository
    {
        private readonly string connectionString;

        public HomeworkDeletionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <inheritdoc />
        public async Task DeleteAll()
        {
            const string deleteGenotypes = "DELETE FROM MatchingGenotypes";
            const string deleteImputationSummaries = "DELETE FROM ImputationSummaries";
            const string deleteAllPairs = "DELETE FROM PatientDonorPairs";
            const string deleteAllSets = "DELETE FROM HomeworkSets";

            var sqlCollection = new[]
            {
                deleteGenotypes,
                deleteImputationSummaries,
                deleteAllPairs, 
                deleteAllSets
            };

            await using (var connection = new SqlConnection(connectionString))
            {
                foreach (var sql in sqlCollection)
                {
                    await connection.ExecuteAsync(sql);
                }
            }
        }
    }
}