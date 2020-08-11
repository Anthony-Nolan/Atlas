using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface IActiveRepositoryFactory : ITransientRepositoryFactory
    {
        IDonorSearchRepository GetDonorSearchRepository();
        IPreFilteredDonorSearchRepository GetPreFilteredDonorSearchRepository();
        IDonorManagementLogRepository GetDonorManagementLogRepository();
    }

    public class ActiveRepositoryFactory : TransientRepositoryFactoryBase, IActiveRepositoryFactory
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public ActiveRepositoryFactory(ActiveTransientSqlConnectionStringProvider activeConnectionStringProvider, IMatchingAlgorithmImportLogger logger)
            : base(activeConnectionStringProvider, logger)
        {
        }

        public IDonorSearchRepository GetDonorSearchRepository()
        {
            return new DonorSearchRepository(ConnectionStringProvider, logger);
        }

        public IPreFilteredDonorSearchRepository GetPreFilteredDonorSearchRepository()
        {
            return new PreFilteredDonorSearchRepository(ConnectionStringProvider);
        }

        public IDonorManagementLogRepository GetDonorManagementLogRepository()
        {
            return new DonorManagementLogRepository(ConnectionStringProvider);
        }
    }
}