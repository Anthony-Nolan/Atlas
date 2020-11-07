using System.Collections.Generic;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using static EnumStringValues.EnumExtensions;

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
        private readonly IMatchingAlgorithmImportLogger logger;

        public StaticallyChosenDatabaseRepositoryFactory(
            StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringProviderFactory,
            IMatchingAlgorithmImportLogger logger
        )
        {
            this.connectionStringProviderFactory = connectionStringProviderFactory;
            this.logger = logger;
        }

        private class AvailableRepositories
        {
            public IDonorManagementLogRepository DonorManagementLog { get; set; }
            public IDonorInspectionRepository DonorInspection { get; set; }
            public IPGroupRepository PGroup { get; set; }
            public IHlaNamesRepository HlaNames { get; set; }
            public IDonorUpdateRepository DonorUpdate { get; set; }
        }

        readonly Dictionary<TransientDatabase, AvailableRepositories> cachedRepositories =
            EnumerateValues<TransientDatabase>().ToDictionary(db => db, db => new AvailableRepositories());

        /* **********************************************************
         * ** All of this is working around the lack of            **
         * ** Func<TArg, TDependency> support in MS DI.            **
         * ** If we ever migrate to some other DI framework, which **
         * ** DOES support that dependency declaration structure,  **
         * ** then it can all go away :)                           **
         * ********************************************************** */

        private IConnectionStringProvider GetConnectionStringProvider(TransientDatabase targetDatabase) =>
            connectionStringProviderFactory.GenerateConnectionStringProvider(targetDatabase);

        public IDonorManagementLogRepository GetDonorManagementLogRepositoryForDatabase(TransientDatabase targetDatabase)
        {
            var available = cachedRepositories[targetDatabase];
            return available.DonorManagementLog ??
                   (available.DonorManagementLog = new DonorManagementLogRepository(GetConnectionStringProvider(targetDatabase)));
        }

        public IDonorInspectionRepository GetDonorInspectionRepositoryForDatabase(TransientDatabase targetDatabase)
        {
            var available = cachedRepositories[targetDatabase];
            return available.DonorInspection ??
                   (available.DonorInspection = new DonorInspectionRepository(GetConnectionStringProvider(targetDatabase)));
        }

        public IPGroupRepository GetPGroupRepositoryForDatabase(TransientDatabase targetDatabase)
        {
            var available = cachedRepositories[targetDatabase];
            return available.PGroup ?? (available.PGroup = new PGroupRepository(GetConnectionStringProvider(targetDatabase)));
        }

        public IHlaNamesRepository GetHlaNamesRepositoryForDatabase(TransientDatabase targetDatabase)
        {
            var available = cachedRepositories[targetDatabase];
            return available.HlaNames ?? (available.HlaNames = new HlaNamesRepository(GetConnectionStringProvider(targetDatabase)));
        }

        public IDonorUpdateRepository GetDonorUpdateRepositoryForDatabase(TransientDatabase targetDatabase)
        {
            var available = cachedRepositories[targetDatabase];
            return available.DonorUpdate ?? (available.DonorUpdate = new DonorUpdateRepository(GetHlaNamesRepositoryForDatabase(targetDatabase),
                GetConnectionStringProvider(targetDatabase), logger));
        }
    }
}