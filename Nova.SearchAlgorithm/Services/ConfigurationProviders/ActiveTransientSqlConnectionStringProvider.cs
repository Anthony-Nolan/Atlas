using System;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public class ActiveTransientSqlConnectionStringProvider : TransientSqlConnectionStringProvider
    {
        public ActiveTransientSqlConnectionStringProvider(
            ConnectionStrings connectionStrings,
            IActiveDatabaseProvider activeDatabaseProvider) : base(connectionStrings, activeDatabaseProvider)
        {
        }

        public override string GetConnectionString()
        {
            var database = ActiveDatabaseProvider.GetActiveDatabase();

            switch (database)
            {
                case TransientDatabase.DatabaseA:
                    return ConnectionStrings.TransientA;
                case TransientDatabase.DatabaseB:
                    return ConnectionStrings.TransientB;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}