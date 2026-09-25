using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
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
        ISubjectGenotypeSetRepository GetSubjectGenotypeSetRepository();
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
            return new HlaImportRepository(GetHlaNamesRepository(), GetPGroupRepository(), ConnectionStringProvider);
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
            return new DonorUpdateRepository(GetHlaImportRepository(), ConnectionStringProvider, logger);
        }

        /// <summary>
        /// On the base rather than on one of the two derived factories: the data refresh writes these rows to the
        /// dormant database and a search reads them from the active one, so both sides need it.
        /// </summary>
        public ISubjectGenotypeSetRepository GetSubjectGenotypeSetRepository()
        {
            return new SubjectGenotypeSetRepository(ConnectionStringProvider);
        }
    }
}