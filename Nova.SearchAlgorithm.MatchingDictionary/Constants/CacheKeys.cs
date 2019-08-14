using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.MatchingDictionary.Constants
{
    public static class CacheKeys
    {
        private const string CacheKeyAntigensPrefix = "Antigens";

        public static string AntigenCacheKey(Locus locus)
        {
            return $"{CacheKeyAntigensPrefix}_{locus}";
        }
    }
}