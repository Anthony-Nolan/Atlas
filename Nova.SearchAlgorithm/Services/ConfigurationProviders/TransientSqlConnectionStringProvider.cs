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
        protected readonly IActiveDatabaseProvider ActiveDatabaseProvider;

        protected readonly ConnectionStrings ConnectionStrings;

        protected TransientSqlConnectionStringProvider(ConnectionStrings connectionStrings, IActiveDatabaseProvider activeDatabaseProvider)
        {
            ConnectionStrings = connectionStrings;
            ActiveDatabaseProvider = activeDatabaseProvider;
        }

        public abstract string GetConnectionString();
    }
}