using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders
{
    /// <summary>
    /// Provides the connection string needed to query the Active non-persistent sql database.
    /// This database can be switched at runtime, hence "which DB to use" needs to be resolved dynamically, rather than just using and AppSetting.
    /// </summary>
    public class ActiveTransientSqlConnectionStringProvider : DynamicallyChosenTransientSqlConnectionStringProvider
    {
        public ActiveTransientSqlConnectionStringProvider(ConnectionStrings connectionStrings, IActiveDatabaseProvider activeDatabaseProvider)
            : base(connectionStrings, activeDatabaseProvider)
        { }

        protected override TransientDatabase DatabaseType() => ActiveDatabaseProvider.GetActiveDatabase();
    }
}