using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.Utils.Models;

namespace Atlas.MatchingAlgorithm.Data.Helpers
{
    internal static class MatchingTableNameHelper
    {
        public static string MatchingTableName(Locus locus)
        {
            return "MatchingHlaAt" + locus;
        }
    }
}