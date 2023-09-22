using System;

namespace Atlas.MatchingAlgorithm.Test.Integration.TestHelpers
{
    /// <summary>
    /// Shared auto-incrementer to generate unique donor ids across all integration test fixtures
    /// </summary>
    public static class DonorIdGenerator
    {
        private static int _nextId;
        
        public static int NextId()
        {
            return ++_nextId;
        }

        public static string NewExternalCode => Guid.NewGuid().ToString();
    }
}