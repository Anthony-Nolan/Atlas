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
            const string deleteAllPairs = "DELETE FROM PatientDonorPairs";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(deleteAllPairs);
            }

            const string deleteAllSets = "DELETE FROM HomeworkSets";

            await using (var connection = new SqlConnection(connectionString))
            {
                await connection.ExecuteAsync(deleteAllSets);
            }
        }
    }
}