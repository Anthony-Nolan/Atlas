using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    /// <summary>
    /// Manages the partition and row key values to be used with HLA lookup tables.
    /// </summary>
    internal static class HlaLookupTableKeyManager
    {
        public static IEnumerable<string> GetTablePartitionKeys()
        {
            return LocusSettings
                .AllLoci
                .Select(locus => locus.ToString());
        }

        public static string GetEntityPartitionKey(Locus locus)
        {
            return locus.ToString();
        }

        public static string GetEntityRowKey(string lookupName, TypingMethod typingMethod)
        {
            return $"{lookupName}-{typingMethod.ToString()}";
        }
    }
}
