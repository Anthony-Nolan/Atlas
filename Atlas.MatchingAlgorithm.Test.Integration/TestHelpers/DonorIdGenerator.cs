using System;
using System.Threading;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    /// <summary>
    /// Shared auto-incrementer to generate unique donor ids across all integration test fixtures, including those running at the same time
    /// </summary>
    public static class DonorIdGenerator
    {
        private static int _nextId;
        
        public static int NextId()
        {
            return Interlocked.Increment(ref _nextId);
        }

        public static string NewExternalCode => Guid.NewGuid().ToString();
    }
}