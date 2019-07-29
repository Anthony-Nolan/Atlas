using System;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Data.Services;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public abstract class TransientSqlConnectionStringProvider : IConnectionStringProvider
    {
        protected readonly IActiveDatabaseProvider ActiveDatabaseProvider;

        private readonly ConnectionStrings connectionStrings;

        protected TransientSqlConnectionStringProvider(ConnectionStrings connectionStrings, IActiveDatabaseProvider activeDatabaseProvider)
        {
            this.connectionStrings = connectionStrings;
            ActiveDatabaseProvider = activeDatabaseProvider;
        }

        public string GetConnectionString()
        {
            var database = DatabaseType();
            return GetConnectionString(database);  
        }

        protected abstract TransientDatabase DatabaseType();

        private string GetConnectionString(TransientDatabase database)
        {
            switch (database)
            {
                case TransientDatabase.DatabaseA:
                    return connectionStrings.TransientA;
                case TransientDatabase.DatabaseB:
                    return connectionStrings.TransientB;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}