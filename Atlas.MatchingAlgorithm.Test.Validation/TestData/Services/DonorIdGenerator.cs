// ReSharper disable InconsistentNaming
using System;
using System.Threading;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    public static class DonorIdGenerator
    {
        private static int nextId = 0;

        // Scenarios run in parallel, so ids are handed out atomically.
        public static int NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        public static string NewExternalCode => Guid.NewGuid().ToString();
    }
}