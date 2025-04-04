using System;

namespace Atlas.MatchingAlgorithm.Data.Persistent.Models
{
    /// <summary>
    /// Used for keeping track of which of two databases should be queried, and for hot-swapping the database used at runtime
    /// </summary>
    public enum TransientDatabase
    {
        DatabaseA,
        DatabaseB
    }

    public static class Extension
    {
        public static TransientDatabase Other(this TransientDatabase thisDb)
        {
            return thisDb switch
            {
                TransientDatabase.DatabaseA => TransientDatabase.DatabaseB,
                TransientDatabase.DatabaseB => TransientDatabase.DatabaseA,
                _ => throw new ArgumentOutOfRangeException(nameof(thisDb), thisDb, null)
            };
        }
    }
}