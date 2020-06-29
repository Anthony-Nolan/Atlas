using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders
{
    /// <summary>
    /// Provides the fixed connection string needed to query a statically specified the non-persistent sql database. i.e. "Database A" or "Database B".
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public class StaticallyChosenTransientSqlConnectionStringProvider : TransientSqlConnectionStringProvider
    {
        public StaticallyChosenTransientSqlConnectionStringProvider(TransientDatabase chosenDatabase, ConnectionStrings connectionStrings) : base(connectionStrings)
        {
            this.chosenDatabase = chosenDatabase;
        }
        private readonly TransientDatabase chosenDatabase;
        protected override TransientDatabase DatabaseType() => chosenDatabase;
    }
}