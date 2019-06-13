namespace Nova.SearchAlgorithm.Data.Persistent.Models
{
    /// <summary>
    /// Used for keeping track of which of two databases should be queried, and for hot-swapping the database used at runtime
    /// </summary>
    public enum TransientDatabase
    {
        DatabaseA,
        DatabaseB
    }
}