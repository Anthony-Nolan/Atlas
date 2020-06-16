namespace Atlas.DonorImport.Data.Repositories
{
    public abstract class DonorRepositoryBase
    {
        protected readonly string ConnectionString;

        protected DonorRepositoryBase(string connectionString)
        {
            this.ConnectionString = connectionString;
        }
    }
}