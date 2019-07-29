using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface IDormantRepositoryFactory : ITransientRepositoryFactory
    {
        IDonorImportRepository GetDonorImportRepository();
        IDataRefreshRepository GetDataRefreshRepository();
    }

    public class DormantRepositoryFactory : TransientRepositoryFactoryBase, IDormantRepositoryFactory
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public DormantRepositoryFactory(DormantTransientSqlConnectionStringProvider dormantConnectionStringProvider)
            : base(dormantConnectionStringProvider)
        {
        }

        public IDonorImportRepository GetDonorImportRepository()
        {
            return new DonorImportRepository(GetPGroupRepository(), ConnectionStringProvider);
        }

        public IDataRefreshRepository GetDataRefreshRepository()
        {
            return new DataRefreshRepository(ConnectionStringProvider);
        }
    }
}