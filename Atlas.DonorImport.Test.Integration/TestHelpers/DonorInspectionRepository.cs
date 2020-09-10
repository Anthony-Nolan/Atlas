using System.Threading.Tasks;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Dapper;

namespace Atlas.DonorImport.Test.Integration.TestHelpers
{
    /// <summary>
    /// Contains methods for inspecting donor data that are only necessary in the context of integration tests.
    /// </summary>
    public interface IDonorInspectionRepository : IDonorReadRepository
    {
        Task<Donor> GetDonor(string externalDonorCode);
        Task<int> DonorCount();
    }

    public class DonorInspectionRepository : DonorReadRepository, IDonorInspectionRepository
    {
        public DonorInspectionRepository(string connectionString) : base(connectionString) { }

        public async Task<Donor> GetDonor(string externalDonorCode)
        {
            await using (var conn = NewConnection())
            {
                return await conn.QuerySingleOrDefaultAsync<Donor>(
                    $"SELECT * FROM {Donor.QualifiedTableName} WHERE ExternalDonorCode = @externalDonorCode",
                    new { externalDonorCode },
                    commandTimeout: 300);
            }
        }

        public async Task<int> DonorCount()
        {
            await using (var conn = NewConnection())
            {
                return await conn.QuerySingleOrDefaultAsync<int>($"SELECT COUNT(*) FROM {Donor.QualifiedTableName}", commandTimeout: 300);
            }
        }
    }
}