using Atlas.Common.Sql.BulkInsert;
using Atlas.DonorImport.Data.Models;

namespace Atlas.DonorImport.Data.Repositories
{
    public interface IDonorImportFailureRepository : IBulkInsertRepository<DonorImportFailure>
    {
    }

    public class DonorImportFailureRepository : BulkInsertRepository<DonorImportFailure>, IDonorImportFailureRepository
    {
        public DonorImportFailureRepository(string connectionString) : base(connectionString, DonorImportFailure.QualifiedTableName)
        {
        }
    }
}
