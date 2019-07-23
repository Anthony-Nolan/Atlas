using System;
using System.Threading.Tasks;
using LazyCache;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Data.Services;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public class TransientSqlConnectionStringProvider : IConnectionStringProvider
    {
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly string connectionStringA;
        private readonly string connectionStringB;
        private readonly IAppCache cache;

        public TransientSqlConnectionStringProvider(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            string connectionStringA,
            string connectionStringB,
            IAppCache cache)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.connectionStringA = connectionStringA;
            this.connectionStringB = connectionStringB;
            this.cache = cache;
        }

        public string GetConnectionString()
        {
            // Caching this rather than fetching every time means that all queries within the lifetime of this class will access the same database,
            // even if the refresh job finishes mid-request.
            // As such it is especially important that this class be injected once per lifetime scope (i.e. singleton per http request)
            var database = cache.GetOrAdd("database", () => dataRefreshHistoryRepository.GetActiveDatabase() ?? TransientDatabase.DatabaseA);

            switch (database)
            {
                case TransientDatabase.DatabaseA:
                    return connectionStringA;
                case TransientDatabase.DatabaseB:
                    return connectionStringB;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}