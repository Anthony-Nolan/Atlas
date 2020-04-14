using Atlas.MatchingAlgorithm.Data.Services;

namespace Atlas.MatchingAlgorithm.Data.Repositories
{
    /// <summary>
    /// Base class for all transient database repositories.
    /// Handles connection string provider - all transient repositories need a connection string provider to know which of the two databases to use
    /// within the current scope
    /// </summary>
    public abstract class Repository
    {
        protected readonly IConnectionStringProvider ConnectionStringProvider;

        protected Repository(IConnectionStringProvider connectionStringProvider)
        {
            ConnectionStringProvider = connectionStringProvider;
        }
    }
}