using System.Text;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.MatchPrediction.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchPrediction.Test.Integration.TestHelpers
{
    /// <summary>
    /// Contains methods for inspecting haplotype frequency data that are only necessary in the context of integration tests.
    /// </summary>
    internal interface IHaplotypeFrequencyInspectionRepository
    {
        Task<int> ActiveSetCount(string registryCode, string ethnicityCode);
        Task<int> HaplotypeFrequencyCount(int setId);
        Task<HaplotypeFrequency> GetFirstHaplotypeFrequency(int setId);
        Task<int> GetLatestSetId();
        Task<bool> HasHaplotypeFrequencies(int setId);
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
            var sqlBuilder = new StringBuilder($"SELECT COUNT(*) FROM {HaplotypeFrequencySet.QualifiedTableName} WHERE Active = 1 AND ");
            sqlBuilder.Append(registryCode.IsNullOrEmpty() ? "RegistryCode IS NULL" : "RegistryCode = @RegistryCode");
            sqlBuilder.Append(" AND ");
            sqlBuilder.Append(ethnicityCode.IsNullOrEmpty() ? "EthnicityCode IS NULL" : "EthnicityCode = @EthnicityCode");

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<int>(
                    sqlBuilder.ToString(),
                    param: new {RegistryCode = registryCode, EthnicityCode = ethnicityCode},
                    commandTimeout: 300);
            }
        }

        public async Task<int> HaplotypeFrequencyCount(int setId)
        {
            var sql = $"SELECT COUNT(*) FROM {HaplotypeFrequency.QualifiedTableName} WHERE Set_Id = @SetId";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<int>(
                    sql,
                    param: new {SetId = setId},
                    commandTimeout: 300);
            }
        }

        public async Task<HaplotypeFrequency> GetFirstHaplotypeFrequency(int setId)
        {
            var sql = $"SELECT TOP 1 * FROM {HaplotypeFrequency.QualifiedTableName} WHERE Set_Id = @SetId";

            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<HaplotypeFrequency>(
                    sql,
                    param: new {SetId = setId},
                    commandTimeout: 300);
            }
        }

        public async Task<int> GetLatestSetId()
        {
            var sql = $"SELECT TOP 1 Id FROM {HaplotypeFrequency.QualifiedTableName} ORDER BY Set_Id DESC";
            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<int>(sql);
            }
        }

        public async Task<bool> HasHaplotypeFrequencies(int setId)
        {
            var sql = $"SELECT TOP 1 1 FROM {HaplotypeFrequency.QualifiedTableName} WHERE Set_Id = @{nameof(setId)}";  // using TOP 1 1 as it's faster than COUNT(*) and fits the needs for this method
            await using (var conn = new SqlConnection(connectionString))
            {
                return await conn.QuerySingleOrDefaultAsync<int>(sql, new { setId }) == 1;
            }
        }
    }
}