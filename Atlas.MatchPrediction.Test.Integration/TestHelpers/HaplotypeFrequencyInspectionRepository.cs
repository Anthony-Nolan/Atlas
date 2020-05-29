using Dapper;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers
{
    /// <summary>
    /// Contains methods for inspecting haplotype frequency data that are only necessary in the context of integration tests.
    /// </summary>
    internal interface IHaplotypeFrequencyInspectionRepository
    {
        Task<int> ActiveSetCount(string registryCode, string ethnicityCode);
        Task<int> HaplotypeFrequencyCount(int setId);
    }

    internal class HaplotypeFrequencyInspectionRepository : IHaplotypeFrequencyInspectionRepository
    {
        private readonly string connectionString;

        public HaplotypeFrequencyInspectionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> ActiveSetCount(string registryCode, string ethnicityCode)
        {
            var sql = "SELECT COUNT(*) FROM HaplotypeFrequencySets WHERE Active = 1 AND ";
            sql += registryCode.IsNullOrEmpty() ? "RegistryCode IS NULL" : "RegistryCode = @RegistryCode";
            sql += " AND ";
            sql += ethnicityCode.IsNullOrEmpty() ? "EthnicityCode IS NULL" : "EthnicityCode = @EthnicityCode";

            await using var conn = new SqlConnection(connectionString);
            return await conn.QuerySingleOrDefaultAsync<int>(
                sql,
                param: new { RegistryCode = registryCode , EthnicityCode = ethnicityCode },
                commandTimeout: 300);
        }

        public async Task<int> HaplotypeFrequencyCount(int setId)
        {
            const string sql = "SELECT COUNT(*) FROM HaplotypeFrequencies WHERE Set_Id = @SetId";

            await using var conn = new SqlConnection(connectionString);
            return await conn.QuerySingleOrDefaultAsync<int>(
                sql,
                param: new { SetId = setId },
                commandTimeout: 300);
        }
    }
}