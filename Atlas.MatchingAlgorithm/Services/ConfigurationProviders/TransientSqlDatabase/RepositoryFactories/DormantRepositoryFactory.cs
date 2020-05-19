using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
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