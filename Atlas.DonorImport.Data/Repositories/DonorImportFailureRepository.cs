using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Models;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Dapper;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportFailureRepository : IBulkInsertRepository<DonorImportFailure>
    {
        Task DeleteDonorImportFailuresBefore(DateTimeOffset dateCutOff);
        Task<IEnumerable<DonorImportFailure>> GetDonorImportFailuresByFileName(string fileName);
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

        public async Task<IEnumerable<DonorImportFailure>> GetDonorImportFailuresByFileName(string fileName)
        {
            var fileNameWithWildCard = $"%{fileName}";
            var sql = @$"SELECT * FROM {DonorImportFailure.QualifiedTableName}
                         WHERE {nameof(DonorImportFailure.UpdateFile)} LIKE @{nameof(fileNameWithWildCard)}";

            await using (var connection = new SqlConnection(ConnectionString))
            {
                return await connection.QueryAsync<DonorImportFailure>(sql, param: new { fileNameWithWildCard }, commandTimeout: 600);
            }
        }
    }
}
