using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface IActiveRepositoryFactory : ITransientRepositoryFactory
    {
        IDonorSearchPhaseOneRepository GetDonorSearchPhaseOneRepository();
        IDonorSearchPhaseTwoRepository GetDonorSearchPhaseTwoRepository();
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

        public IDonorSearchPhaseOneRepository GetDonorSearchPhaseOneRepository()
        {
            return new DonorSearchPhaseOneRepository(ConnectionStringProvider, logger);
        }

        public IDonorSearchPhaseTwoRepository GetDonorSearchPhaseTwoRepository()
        {
            return new DonorSearchPhaseTwoRepository(ConnectionStringProvider);
        }

        public IDonorManagementLogRepository GetDonorManagementLogRepository()
        {
            return new DonorManagementLogRepository(ConnectionStringProvider);
        }
    }
}