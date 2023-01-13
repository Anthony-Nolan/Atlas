using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;

namespace Atlas.HlaMetadataDictionary.Services.AzureStorage
{
    /// <summary>
    /// Manages the partition and row key values to be used with HLA Metadata tables.
    /// </summary>
    internal static class HlaMetadataTableKeyManager
    {
        public static string GetPartitionKey(Locus locus)
        {
            return locus.ToString();
        }

        public static string GetRowKey(string lookupName, TypingMethod typingMethod)
        {
            return $"{lookupName}-{typingMethod.ToString()}";
        }
    }
}
