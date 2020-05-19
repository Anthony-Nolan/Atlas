using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders
{
    /// <summary>
    /// Provides the connection string needed to query the non-persistent sql database.
    /// This database can be switched at runtime, hence the necessary service rather than an application setting
    /// </summary>
    public class DormantTransientSqlConnectionStringProvider : TransientSqlConnectionStringProvider
    {
        public DormantTransientSqlConnectionStringProvider(
            ConnectionStrings connectionStrings,
            IActiveDatabaseProvider activeDatabaseProvider
        ) : base(connectionStrings, activeDatabaseProvider)
        {
        }
        
        protected override TransientDatabase DatabaseType()
        {
            return ActiveDatabaseProvider.GetDormantDatabase();
        }
    }
}