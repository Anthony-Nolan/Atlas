using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorReadRepository
    {
        public IEnumerable<Donor> GetAllDonors();
        public Task<Dictionary<string, Donor>> GetDonorsByExternalDonorCodes(IEnumerable<string> externalDonorCodes);
    }

    public class DonorReadRepository : DonorRepositoryBase, IDonorReadRepository
    {
        /// <inheritdoc />
        public DonorReadRepository(string connectionString) : base(connectionString)
        {
        }

        public IEnumerable<Donor> GetAllDonors()
        {
            var sql = $"SELECT {DonorInsertDataTableColumnNames.StringJoin(",")} FROM Donors";
            using (var connection = new SqlConnection(ConnectionString))
            {
                // With "buffered: true" this will load all donors into memory before returning.
                // We may want to consider streaming this if we have issues running out of memory in this approach.  
                // Pro: Smaller memory footprint.
                // Con: Longer open connection, consumer can cause timeouts by not fully enumerating.
                return connection.Query<Donor>(sql, buffered: true);
            }
        }

        public async Task<Dictionary<string, Donor>> GetDonorsByExternalDonorCodes(IEnumerable<string> externalDonorCodes)
        {
            var sql = @$"
SELECT {DonorInsertDataTableColumnNames.StringJoin(",")} FROM Donors
WHERE ExternalDonorCode IN @codes
";
            await using (var connection = new SqlConnection(ConnectionString))
            {
                var donors = await connection.QueryAsync<Donor>(sql, new {codes = externalDonorCodes});
                return donors.ToDictionary(d => d.ExternalDonorCode, d => d);
            }
        }
    }
}