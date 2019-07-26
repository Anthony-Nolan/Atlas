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
            ConnectionStrings connectionStrings,
            IActiveDatabaseProvider activeDatabaseProvider
        ) : base(connectionStrings, activeDatabaseProvider)
        {
        }

        public override string GetConnectionString()
        {
            var database = ActiveDatabaseProvider.GetDormantDatabase();
            return GetConnectionString(database);
        }
    }
}