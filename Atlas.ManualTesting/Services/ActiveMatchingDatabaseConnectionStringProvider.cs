using Atlas.MatchingAlgorithm.Data.Services;

namespace Atlas.ManualTesting.Services
{
    /// <summary>
    /// Developer running manual testing project will know which database is currently active, so keeping thing simple here
    /// by allowing the connecting string to set via the Functions app settings file, instead of using the existing matching algorithm project's
    /// connection string providers.
    /// </summary>
    public interface IActiveMatchingDatabaseConnectionStringProvider : IConnectionStringProvider {}

    internal class ActiveMatchingDatabaseConnectionStringProvider : IActiveMatchingDatabaseConnectionStringProvider
    {
        private readonly string connectionString;

        public ActiveMatchingDatabaseConnectionStringProvider(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public string GetConnectionString()
        {
            return connectionString;
        }
    }
}
