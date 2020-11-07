using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Services;

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
        protected readonly IMatchingAlgorithmImportLogger logger;

        protected TransientRepositoryFactoryBase(IConnectionStringProvider connectionStringProvider, IMatchingAlgorithmImportLogger logger)
        {
            this.ConnectionStringProvider = connectionStringProvider;
            this.logger = logger;
        }

        public IHlaNamesRepository GetHlaNamesRepository()
        {
            return new HlaNamesRepository(ConnectionStringProvider);
        }

        /// <inheritdoc />
        public IHlaImportRepository GetHlaImportRepository()
        {
            return new HlaImportRepository(ConnectionStringProvider);
        }

        public IPGroupRepository GetPGroupRepository()
        {
            return new PGroupRepository(ConnectionStringProvider);
        }

        public IDonorInspectionRepository GetDonorInspectionRepository()
        {
            return new DonorInspectionRepository(ConnectionStringProvider);
        }

        public IDonorUpdateRepository GetDonorUpdateRepository()
        {
            return new DonorUpdateRepository(GetPGroupRepository(), ConnectionStringProvider, logger);
        }
    }
}