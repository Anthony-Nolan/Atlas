// ReSharper disable InconsistentNaming
namespace Nova.SearchAlgorithm.Test.Validation.TestData
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