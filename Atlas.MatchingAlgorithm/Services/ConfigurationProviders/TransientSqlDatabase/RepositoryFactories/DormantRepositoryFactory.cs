using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories
{
    public interface IDormantRepositoryFactory : ITransientRepositoryFactory
    {
        IDonorImportRepository GetDonorImportRepository();
        IDataRefreshRepository GetDataRefreshRepository();
        IDonorManagementLogRepository GetDonorManagementLogRepository();

        /// <summary>
        /// For the donor genotype precomputation stage of the data refresh. The workers and the sweeps take the database
        /// from the refresh record, so they use <see cref="IStaticallyChosenDatabaseRepositoryFactory"/> instead.
        /// </summary>
        IDonorGenotypePrecomputationRepository GetDonorGenotypePrecomputationRepository();
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

        /// <inheritdoc />
        public IDonorManagementLogRepository GetDonorManagementLogRepository()
        {
            return new DonorManagementLogRepository(ConnectionStringProvider);
        }

        /// <inheritdoc />
        public IDonorGenotypePrecomputationRepository GetDonorGenotypePrecomputationRepository()
        {
            return new DonorGenotypePrecomputationRepository(ConnectionStringProvider);
        }
    }
}