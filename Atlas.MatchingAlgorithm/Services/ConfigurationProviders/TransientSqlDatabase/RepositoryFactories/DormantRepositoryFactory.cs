using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
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
        public DormantRepositoryFactory(
            DormantTransientSqlConnectionStringProvider dormantConnectionStringProvider,
            IMatchingAlgorithmImportLogger logger
            )
            : base(dormantConnectionStringProvider, logger)
        {
        }

        public IDonorImportRepository GetDonorImportRepository()
        {
            return new DonorImportRepository(GetHlaNamesRepository(), ConnectionStringProvider, logger);
        }

        public IDataRefreshRepository GetDataRefreshRepository()
        {
            return new DataRefreshRepository(ConnectionStringProvider);
        }
    }
}