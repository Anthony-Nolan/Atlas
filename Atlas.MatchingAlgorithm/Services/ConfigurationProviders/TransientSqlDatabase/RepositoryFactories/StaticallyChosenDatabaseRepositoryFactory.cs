using Atlas.MatchingAlgorithm.Common.Repositories;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface IStaticallyChosenDatabaseRepositoryFactory
    {
        IDonorManagementLogRepository GetDonorManagementLogRepositoryForDatabase(TransientDatabase targetDatabase);
        IDonorInspectionRepository GetDonorInspectionRepositoryForDatabase(TransientDatabase targetDatabase);
        IDonorUpdateRepository GetDonorUpdateRepositoryForDatabase(TransientDatabase targetDatabase);
        IPGroupRepository GetPGroupRepositoryForDatabase(TransientDatabase targetDatabase);
    }

    public class StaticallyChosenDatabaseRepositoryFactory : IStaticallyChosenDatabaseRepositoryFactory
    {
        private readonly StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringProviderFactory;
        public StaticallyChosenDatabaseRepositoryFactory(StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringProviderFactory)
        {
            this.connectionStringProviderFactory = connectionStringProviderFactory;
        }

        private IConnectionStringProvider GetConnectionStringProvider(TransientDatabase targetDatabase) => connectionStringProviderFactory.GenerateConnectionStringProvider(targetDatabase);

        public IDonorManagementLogRepository GetDonorManagementLogRepositoryForDatabase(TransientDatabase targetDatabase) => new DonorManagementLogRepository(GetConnectionStringProvider(targetDatabase));
        public IDonorInspectionRepository GetDonorInspectionRepositoryForDatabase(TransientDatabase targetDatabase) => new DonorInspectionRepository(GetConnectionStringProvider(targetDatabase));
        public IPGroupRepository GetPGroupRepositoryForDatabase(TransientDatabase targetDatabase) => new PGroupRepository(GetConnectionStringProvider(targetDatabase));

        public IDonorUpdateRepository GetDonorUpdateRepositoryForDatabase(TransientDatabase targetDatabase)
            => new DonorUpdateRepository(
                GetPGroupRepositoryForDatabase(targetDatabase),
                GetConnectionStringProvider(targetDatabase)
                );
    }
}