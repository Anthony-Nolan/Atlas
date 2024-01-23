using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequencySetReadRepository
    {
        Task<HaplotypeFrequencySet> GetActiveHaplotypeFrequencySet(string registryCode, string ethnicityCode);
        Task<IEnumerable<HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySet(int populationId);
    }

    public class HaplotypeFrequencySetReadRepository : IHaplotypeFrequencySetReadRepository
    {
        private readonly string connectionString;

        public HaplotypeFrequencySetReadRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<HaplotypeFrequencySet> GetActiveHaplotypeFrequencySet(string registryCode, string ethnicityCode)
        {
            var sql = @$"SELECT s.* FROM {HaplotypeFrequencySet.QualifiedTableName} s WHERE
                    s.Active = 1 AND
                    ISNULL(s.RegistryCode,'') = ISNULL(@{nameof(registryCode)},'') AND
                    ISNULL(s.ethnicityCode,'') = ISNULL(@{nameof(ethnicityCode)},'')";

            return await RetryConfig.AsyncRetryPolicy.ExecuteAsync(async () =>
            {
                await using (var conn = new SqlConnection(connectionString))
                {
                    var result = await conn.QueryAsync<HaplotypeFrequencySet>(sql, new {registryCode, ethnicityCode});
                    return result.SingleOrDefault();
                }
            });
        }

        public async Task<IEnumerable<HaplotypeFrequencySet>> GetActiveHaplotypeFrequencySet(int populationId)
        {
            var sql = @$"SELECT * FROM {HaplotypeFrequencySet.QualifiedTableName}
                        WHERE Active = 1 AND PopulationId = @{nameof(populationId)}";

            return await RetryConfig.AsyncRetryPolicy.ExecuteAsync(async () =>
            {
                await using (var conn = new SqlConnection(connectionString))
                {
                    return await conn.QueryAsync<HaplotypeFrequencySet>(sql, new { populationId });
                }
            });
        }
    }
}