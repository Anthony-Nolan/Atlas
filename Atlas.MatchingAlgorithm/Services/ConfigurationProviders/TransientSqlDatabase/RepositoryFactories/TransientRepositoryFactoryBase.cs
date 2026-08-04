using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Data.Settings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface ITransientRepositoryFactory
    {
        IHlaNamesRepository GetHlaNamesRepository();

        IHlaImportRepository GetHlaImportRepository();

        IPGroupRepository GetPGroupRepository();

        IDonorInspectionRepository GetDonorInspectionRepository();

        IDonorUpdateRepository GetDonorUpdateRepository();
    }

    public abstract class TransientRepositoryFactoryBase : ITransientRepositoryFactory
    {
        protected readonly IConnectionStringProvider ConnectionStringProvider;
        protected readonly IMatchingAlgorithmImportLogger Logger;
        protected readonly DataRefreshRepositorySettings RepositorySettings;

        protected TransientRepositoryFactoryBase(
            IConnectionStringProvider connectionStringProvider,
            IMatchingAlgorithmImportLogger logger,
            DataRefreshRepositorySettings repositorySettings)
        {
            this.ConnectionStringProvider = connectionStringProvider;
            this.Logger = logger;
            this.RepositorySettings = repositorySettings;
        }

        public IHlaNamesRepository GetHlaNamesRepository()
        {
            return new HlaNamesRepository(ConnectionStringProvider, Logger);
        }

        /// <inheritdoc />
        public IHlaImportRepository GetHlaImportRepository()
        {
            return new HlaImportRepository(GetHlaNamesRepository(), GetPGroupRepository(), ConnectionStringProvider, Logger);
        }

        public IPGroupRepository GetPGroupRepository()
        {
            return new PGroupRepository(ConnectionStringProvider, Logger);
        }

        public IDonorInspectionRepository GetDonorInspectionRepository()
        {
            return new DonorInspectionRepository(ConnectionStringProvider);
        }

        public IDonorUpdateRepository GetDonorUpdateRepository()
        {
            return new DonorUpdateRepository(GetHlaImportRepository(), ConnectionStringProvider, Logger, RepositorySettings);
        }
    }
}