using Microsoft.Data.SqlClient;

namespace Atlas.DonorImport.Data.Repositories
{
    public abstract class DonorRepositoryBase
    {
        protected readonly string ConnectionString;
        protected SqlConnection NewConnection() => new SqlConnection(ConnectionString);

        protected DonorRepositoryBase(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}