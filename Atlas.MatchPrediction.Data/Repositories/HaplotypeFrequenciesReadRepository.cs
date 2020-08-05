using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesReadRepository
    {
        Task<HaplotypeFrequencySet> GetActiveHaplotypeFrequencySet(string registryCode, string ethnicityCode);
    }

    public class HaplotypeFrequenciesReadRepository : IHaplotypeFrequenciesReadRepository
    {
        private readonly string connectionString;

        public HaplotypeFrequenciesReadRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<HaplotypeFrequencySet> GetActiveHaplotypeFrequencySet(string registryCode, string ethnicityCode)
        {
            var sql = @$"SELECT s.* FROM HaplotypeFrequencySets s WHERE
                    s.Active = 1 AND
                    ISNULL(s.RegistryCode,'') = ISNULL(@{nameof(registryCode)},'') AND
                    ISNULL(s.ethnicityCode,'') = ISNULL(@{nameof(ethnicityCode)},'')";

            await using (var conn = new SqlConnection(connectionString))
            {
                var result = await conn.QueryAsync<HaplotypeFrequencySet>(sql, new {registryCode, ethnicityCode});
                return result.SingleOrDefault();
            }
        }
    }
}