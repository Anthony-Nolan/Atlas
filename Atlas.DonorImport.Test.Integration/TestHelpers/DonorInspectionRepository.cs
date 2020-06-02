using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Test.Integration.TestHelpers
{
    /// <summary>
    /// Contains methods for inspecting donor data that are only necessary in the context of integration tests.
    /// </summary>
    public interface IDonorInspectionRepository
    {
        Task<Donor> GetDonor(string externalDonorCode);
        Task<int> DonorCount();
    }

    public class DonorInspectionRepository : IDonorInspectionRepository
    {
        private readonly string connectionString;

        public DonorInspectionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<Donor> GetDonor(string externalDonorCode)
        {
            await using var conn = new SqlConnection(connectionString);
            return await conn.QuerySingleOrDefaultAsync<Donor>(
                "SELECT * FROM Donors WHERE ExternalDonorCode = @externalDonorCode",
                new {externalDonorCode},
                commandTimeout: 300);
        }

        public async Task<int> DonorCount()
        {
            await using var conn = new SqlConnection(connectionString);
            return await conn.QuerySingleOrDefaultAsync<int>("SELECT COUNT(*) FROM Donors", commandTimeout: 300);
        }
    }
}