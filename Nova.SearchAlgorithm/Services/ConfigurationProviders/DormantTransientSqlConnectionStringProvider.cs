using System;
using LazyCache;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public class DormantTransientSqlConnectionStringProvider : TransientSqlConnectionStringProvider
    {
        public DormantTransientSqlConnectionStringProvider(
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            ConnectionStrings connectionStrings,
            IAppCache cache) : base(dataRefreshHistoryRepository, connectionStrings, cache)
        {
        }

        public override string GetConnectionString()
        {
            var database = GetActiveDatabase();

            switch (database)
            {
                case TransientDatabase.DatabaseA:
                    return connectionStrings.TransientB;
                case TransientDatabase.DatabaseB:
                    return connectionStrings.TransientA;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}