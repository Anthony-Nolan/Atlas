using Atlas.MatchingAlgorithm.Settings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders
{
    /// <summary>
    /// Provides a dynamically chosen connection string needed to query a non-persistent sql database, selected based on the respective roles of the Database at the point of ConString Provision
    /// i.e. "the Active Database" or "the Dormant Database".
    /// Since the roles of the databases can be switched at runtime, the appropriate DB needs to be determined dynamically hence being a service rather than an application setting
    /// </summary>
    public abstract class DynamicallyChosenTransientSqlConnectionStringProvider : TransientSqlConnectionStringProvider
    {
        protected readonly IActiveDatabaseProvider ActiveDatabaseProvider;

        protected DynamicallyChosenTransientSqlConnectionStringProvider(ConnectionStrings connectionStrings, IActiveDatabaseProvider activeDatabaseProvider)
            : base (connectionStrings)
        {
            ActiveDatabaseProvider = activeDatabaseProvider;
        }
    }
}