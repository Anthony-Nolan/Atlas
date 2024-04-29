using Atlas.MatchPrediction.Test.Validation.Data.Context;
using Atlas.MatchPrediction.Test.Validation.Data.Models.Homework;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Validation.Data.Repositories.Homework
{
    public interface IImputationSummaryRepository
    {
        Task<int> Add(ImputationSummary imputationSummary);
    }

    public class ImputationSummaryRepository : IImputationSummaryRepository
    {
        private readonly string connectionString;

        public ImputationSummaryRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> Add(ImputationSummary imputationSummary)
        {
            const string sql = $@"
                INSERT INTO {nameof(MatchPredictionValidationContext.ImputationSummaries)} (
                    ExternalSubjectId,
                    HfSetPopulationId,
                    WasRepresented,
                    GenotypeCount,
                    SumOfLikelihoods)
                VALUES (
                @{nameof(ImputationSummary.ExternalSubjectId)},
                @{nameof(ImputationSummary.HfSetPopulationId)},
                @{nameof(ImputationSummary.WasRepresented)},
                @{nameof(ImputationSummary.GenotypeCount)},
                @{nameof(ImputationSummary.SumOfLikelihoods)});
                SELECT CAST(SCOPE_IDENTITY() as int);";

            await using (var connection = new SqlConnection(connectionString))
            {
                return (await connection.QueryAsync<int>(sql, new
                {
                    imputationSummary.ExternalSubjectId,
                    imputationSummary.HfSetPopulationId,
                    imputationSummary.WasRepresented,
                    imputationSummary.GenotypeCount,
                    imputationSummary.SumOfLikelihoods
                })).Single();
            }
        }
    }
}