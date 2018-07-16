using Nova.SearchAlgorithm.MatchingDictionary.HlaTypingInfo;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class HlaLookupTableHelper
    {
        public static IEnumerable<string> GetTablePartitions()
        {
            return PermittedLocusNames
                .GetPermittedMatchLoci()
                .Select(matchLocus => matchLocus.ToString());
        }

        public static string GetEntityPartition(MatchLocus matchLocus)
        {
            return matchLocus.ToString();
        }

        public static string GetEntityRowKey(string lookupName, TypingMethod typingMethod)
        {
            return $"{lookupName}-{typingMethod.ToString()}";
        }
    }
}
