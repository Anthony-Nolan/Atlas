using System;
using Atlas.Common.Caching;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using LazyCache;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase
{
    public interface IActiveDatabaseProvider
    {
        TransientDatabase GetActiveDatabase();
        TransientDatabase GetDormantDatabase();
    }

    public class ActiveDatabaseProvider : IActiveDatabaseProvider
    {
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAppCache cache;

        public ActiveDatabaseProvider(IDataRefreshHistoryRepository dataRefreshHistoryRepository, ITransientCacheProvider cacheProvider)
        {
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            cache = cacheProvider.Cache;
        }

        public TransientDatabase GetActiveDatabase()
        {
            // Caching this rather than fetching every time means that all queries within the lifetime of this class will access the same database,
            // even if the refresh job finishes mid-request.
            // As such it is especially important that this class be injected once per lifetime scope (i.e. singleton per http request)
            return cache.GetOrAdd("database", () => dataRefreshHistoryRepository.GetActiveDatabase() ?? TransientDatabase.DatabaseA);
        }

        public TransientDatabase GetDormantDatabase()
        {
            return GetActiveDatabase().Other();
        }
    }
}