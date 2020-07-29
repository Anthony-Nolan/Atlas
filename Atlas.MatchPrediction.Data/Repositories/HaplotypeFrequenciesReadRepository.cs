using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Data.Repositories
{
    public interface IHaplotypeFrequenciesReadRepository
    {
        Task<int?> GetActiveHaplotypeSetId(string registryCode, string ethnicityCode);
        Task<IReadOnlyCollection<HaplotypeFrequency>> GetActiveHaplotypeFrequencies(string registryCode, string ethnicityCode);
    }

    public class HaplotypeFrequenciesReadRepository : IHaplotypeFrequenciesReadRepository
    {
        private readonly string connectionString;

        public HaplotypeFrequenciesReadRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int?> GetActiveHaplotypeSetId(string registryCode, string ethnicityCode)
        {
            var sql = @$"SELECT s.Id FROM HaplotypeFrequencySets s WHERE
                    s.Active = 1 AND
                    ISNULL(s.RegistryCode,'') = ISNULL(@{nameof(registryCode)},'') AND
                    ISNULL(s.ethnicityCode,'') = ISNULL(@{nameof(ethnicityCode)},'')";

            await using (var conn = new SqlConnection(connectionString))
            {
                var result = await conn.QueryAsync<HaplotypeFrequencySet>(sql, new {registryCode, ethnicityCode});
                return result.SingleOrDefault()?.Id;
            }
        }

        public async Task<IReadOnlyCollection<HaplotypeFrequency>> GetActiveHaplotypeFrequencies(string registryCode, string ethnicityCode)
        {
            var sql = @$"SELECT h.*
                FROM HaplotypeFrequencySets s
                JOIN HaplotypeFrequencies h
                ON s.Id = h.Set_Id
                WHERE
                    s.Active = 1 AND
                    ISNULL(s.RegistryCode,'') = ISNULL(@{nameof(registryCode)},'') AND
                    ISNULL(s.ethnicityCode,'') = ISNULL(@{nameof(ethnicityCode)},'')";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<HaplotypeFrequency>(sql, new {registryCode, ethnicityCode}, commandTimeout: 600)).ToList();
            }
        }
    }
}