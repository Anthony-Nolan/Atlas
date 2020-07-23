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
        Task<IReadOnlyCollection<HaplotypeFrequency>> GetActiveGlobalHaplotypeFrequencies();
    }

    public class HaplotypeFrequenciesReadRepository : IHaplotypeFrequenciesReadRepository
    {
        private readonly string connectionString;

        public HaplotypeFrequenciesReadRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IReadOnlyCollection<HaplotypeFrequency>> GetActiveGlobalHaplotypeFrequencies()
        {
            const string sql = @"SELECT h.*
                FROM HaplotypeFrequencySets s
                JOIN HaplotypeFrequencies h
                ON s.Id = h.Set_Id
                WHERE s.Active = 1 AND s.RegistryCode IS NULL AND s.EthnicityCode IS NULL";

            await using (var conn = new SqlConnection(connectionString))
            {
                return (await conn.QueryAsync<HaplotypeFrequency>(sql, commandTimeout: 600)).ToList();
            }
        }
    }
}