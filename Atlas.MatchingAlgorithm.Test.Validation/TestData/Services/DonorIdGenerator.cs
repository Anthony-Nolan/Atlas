// ReSharper disable InconsistentNaming
namespace Atlas.MatchingAlgorithm.Test.Validation.TestData.Services
{
    public static class DonorIdGenerator
    {
        private static int nextId = 0;

        public static int NextId()
        {
            return ++nextId;
        }
    }
}