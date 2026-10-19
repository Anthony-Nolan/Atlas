namespace Atlas.MatchingAlgorithm.Services.Debug.Precompute;

/// <summary>Which of the two transient matching databases a debug request reads.</summary>
public enum TransientDatabaseRole
{
    /// <summary>The database that search reads: the database of the last successful data refresh.</summary>
    Active,

    /// <summary>
    /// The other database, which Data Refresh writes to. Its data is at the nomenclature version of the latest refresh that
    /// wrote to it.
    /// </summary>
    Dormant
}
