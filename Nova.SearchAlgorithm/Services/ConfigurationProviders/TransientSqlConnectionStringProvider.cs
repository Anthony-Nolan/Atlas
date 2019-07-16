using System;
using System.Threading.Tasks;
using LazyCache;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Data.Services;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public abstract class TransientSqlConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        protected readonly ConnectionStrings connectionStrings;
        private readonly IAppCache cache;

        protected TransientSqlConnectionStringProvider(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            ConnectionStrings connectionStrings,
            IAppCache cache)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.connectionStrings = connectionStrings;
            this.cache = cache;
        }

        public abstract string GetConnectionString();

        protected TransientDatabase GetActiveDatabase()
        {
            // Caching this rather than fetching every time means that all queries within the lifetime of this class will access the same database,
            // even if the refresh job finishes mid-request.
            // As such it is especially important that this class be injected once per lifetime scope (i.e. singleton per http request)
            return cache.GetOrAdd("database", () => dataRefreshHistoryRepository.GetActiveDatabase() ?? TransientDatabase.DatabaseA);
        }
    }
}