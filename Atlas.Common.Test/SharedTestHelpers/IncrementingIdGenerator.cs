namespace Atlas.Common.Test.SharedTestHelpers
{
    /// <summary>
    /// Shared auto-incrementer to generate unique ids across all integration test fixtures.
    /// Useful for generation of test data with unique constraints, e.g. donor ids.
    /// </summary>
    public static class IncrementingIdGenerator
    {
        private static int nextId = 0;

        public static int NextIntId()
        {
            return ++nextId;
        }

        public static string NextStringId(string prefix = null)
        {
            var stringId = NextIntId().ToString();
            
            return prefix != null ? $"{prefix}{stringId}" : stringId;
        }
    }
}