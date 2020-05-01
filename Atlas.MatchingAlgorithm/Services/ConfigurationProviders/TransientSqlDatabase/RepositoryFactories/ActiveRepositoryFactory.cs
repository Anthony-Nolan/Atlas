using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface IActiveRepositoryFactory : ITransientRepositoryFactory
    {
        IDonorSearchRepository GetDonorSearchRepository();
        IDonorManagementLogRepository GetDonorManagementLogRepository();
    }

    public class ActiveRepositoryFactory : TransientRepositoryFactoryBase, IActiveRepositoryFactory
    {
        private readonly ILogger logger;

        // ReSharper disable once SuggestBaseTypeForParameter
        public ActiveRepositoryFactory(ActiveTransientSqlConnectionStringProvider activeConnectionStringProvider, ILogger logger)
            : base(activeConnectionStringProvider)
        {
            this.logger = logger;
        }

        public IDonorSearchRepository GetDonorSearchRepository()
        {
            return new DonorSearchRepository(ConnectionStringProvider, logger);
        }

        public IDonorManagementLogRepository GetDonorManagementLogRepository()
        {
            return new DonorManagementLogRepository(ConnectionStringProvider);
        }
    }
}