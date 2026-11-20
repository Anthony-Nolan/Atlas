// ReSharper disable InconsistentNaming
using System;
using System.Threading;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    /// <summary>
    /// Unique donor ids across all scenarios, including those running at the same time.
    /// </summary>
    public static class DonorIdGenerator
    {
        private static int nextId = 0;

        public static int NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        public static string NewExternalCode => Guid.NewGuid().ToString();
    }
}