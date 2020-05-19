using Atlas.Common.GeneticData;

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