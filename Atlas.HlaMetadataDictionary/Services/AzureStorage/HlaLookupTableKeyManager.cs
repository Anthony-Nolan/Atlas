using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using static EnumStringValues.EnumExtensions;

namespace Atlas.HlaMetadataDictionary.Services.AzureStorage
{
    /// <summary>
    /// Manages the partition and row key values to be used with HLA lookup tables.
    /// </summary>
    internal static class HlaLookupTableKeyManager
    {
        public static IEnumerable<string> GetTablePartitionKeys()
        {
            return EnumerateValues<Locus>().Select(locus => locus.ToString());
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
