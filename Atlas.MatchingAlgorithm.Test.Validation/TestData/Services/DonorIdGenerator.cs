// ReSharper disable InconsistentNaming
using System;

namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    public static class DonorIdGenerator
    {
        private static int nextId = 0;

        public static int NextId()
        {
            return ++nextId;
        }

        public static string NewExternalCode => Guid.NewGuid().ToString();
    }
}