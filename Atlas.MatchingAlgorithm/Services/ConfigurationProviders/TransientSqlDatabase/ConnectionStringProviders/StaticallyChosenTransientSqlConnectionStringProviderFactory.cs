using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public class StaticallyChosenTransientSqlConnectionStringProviderFactory
    {
        public StaticallyChosenTransientSqlConnectionStringProviderFactory(ConnectionStrings connectionStrings)
        {
            this.connectionStrings = connectionStrings;
        }
        private readonly ConnectionStrings connectionStrings;

        public IConnectionStringProvider GenerateConnectionStringProvider(TransientDatabase chosenDatabase)
        {
            return new StaticallyChosenTransientSqlConnectionStringProvider(chosenDatabase, connectionStrings);
        }
    }
}