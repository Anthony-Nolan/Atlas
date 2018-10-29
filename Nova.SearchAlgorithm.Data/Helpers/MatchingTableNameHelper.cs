using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Data.Helpers
{
    internal static class MatchingTableNameHelper
    {
        public static string MatchingTableName(Locus locus)
        {
            return "MatchingHlaAt" + locus;
        }
    }
}