using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Models;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System;
using Dapper;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportFailureRepository : IBulkInsertRepository<DonorImportFailure>
    {
        Task DeleteDonorImportFailuresBefore(DateTimeOffset dateCutOff);
    }

    public class DonorImportFailureRepository : BulkInsertRepository<DonorImportFailure>, IDonorImportFailureRepository
    {
        public DonorImportFailureRepository(string connectionString) : base(connectionString, DonorImportFailure.QualifiedTableName)
        {
        }

        public async Task DeleteDonorImportFailuresBefore(DateTimeOffset cutOffDate)
        {
            var sql = @$"DELETE FROM {DonorImportFailure.QualifiedTableName}
                            WHERE {nameof(DonorImportFailure.FailureTime)} < @{nameof(cutOffDate)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, param: new { cutOffDate }, commandTimeout: 600);
            }
        }
    }
}
