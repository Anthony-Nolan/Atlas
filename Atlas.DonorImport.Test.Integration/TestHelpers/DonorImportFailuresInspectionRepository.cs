using Atlas.DonorImport.Data.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Test.Integration.TestHelpers
{
    internal interface IDonorImportFailuresInspectionRepository
    {
        Task<IEnumerable<DonorImportFailure>> GetFailuresByFilename(string filename);
        Task CreateRecords(int count, DateTime date);
        Task<IEnumerable<DonorImportFailure>> GetAll();
    }


    internal class DonorImportFailuresInspectionRepository : IDonorImportFailuresInspectionRepository
    {
        private readonly string connectionString;

        public DonorImportFailuresInspectionRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task CreateRecords(int count, DateTime date)
        {
            await using var db = new SqlConnection(connectionString);

            while (count > 0)
            {
                await db.ExecuteAsync($"insert into Donors.DonorImportFailures(FailureTime) values (@{nameof(date)})", new { date });
                count--;
            }

        }

        public async Task<IEnumerable<DonorImportFailure>> GetAll()
        {
            await using var db = new SqlConnection(connectionString);

            return await db.QueryAsync<DonorImportFailure>("select * from Donors.DonorImportFailures");
        }

        public async Task<IEnumerable<DonorImportFailure>> GetFailuresByFilename(string filename)
        {
            await using var db = new SqlConnection(connectionString);

            return await db
                .QueryAsync<DonorImportFailure>($"select * from Donors.DonorImportFailures where UpdateFile = @{nameof(filename)}", new { filename });
        }
    }
}
