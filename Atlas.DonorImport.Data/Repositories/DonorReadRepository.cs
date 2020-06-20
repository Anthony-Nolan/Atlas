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
        public IEnumerable<Donor> StreamAllDonors();
        public Task<Dictionary<string, Donor>> GetDonorsByExternalDonorCodes(IEnumerable<string> externalDonorCodes);
    }

    public class DonorReadRepository : DonorRepositoryBase, IDonorReadRepository
    {
        /// <inheritdoc />
        public DonorReadRepository(string connectionString) : base(connectionString)
        {
        }

        public IEnumerable<Donor> StreamAllDonors()
        {
            var sql = $"SELECT {Donor.InsertionDataTableColumnNames.StringJoin(",")} FROM Donors";
            using (var connection = new SqlConnection(ConnectionString))
            {
                // With "buffered: false" this should avoid loading all donors into memory before returning.
                // This is necessary because we start to have issues running out of memory on a dataset of around 2M donors.
                // Pro: Smaller memory footprint.
                // Con: Longer open connection, could cause timeouts if not by not fully consumed in time.
                var donorStream = connection.Query<Donor>(sql, buffered: false, commandTimeout: 7200);

                // Unfortunately, if you don't do this, then the connection gets closed as soon as
                // you return the lazy enumerable, which then kills the query. So you have to do this, 
                // which will ensure that the connection isn't closed until the end of the stream.
                foreach (var donor in donorStream)
                {
                    yield return donor;
                }
            }
        }

        public async Task<Dictionary<string, Donor>> GetDonorsByExternalDonorCodes(IEnumerable<string> externalDonorCodes)
        {
            var sql = @$"
SELECT {Donor.InsertionDataTableColumnNames.StringJoin(",")} FROM Donors
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