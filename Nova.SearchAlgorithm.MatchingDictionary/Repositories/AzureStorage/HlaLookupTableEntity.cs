using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class HlaLookupTableEntity : TableEntity
    {
        public string SerialisedHlaLookupResult { get; set; }

        public HlaLookupTableEntity() { }

        public HlaLookupTableEntity(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod) 
            : base(GetPartition(matchLocus), GetRowKey(lookupName, typingMethod))
        {
        }

        public static string GetPartition(MatchLocus matchLocus)
        {
            return matchLocus.ToString();
        }

        public static string GetRowKey(string lookupName, TypingMethod typingMethod)
        {
            return $"{lookupName}-{typingMethod.ToString()}";
        }
    }
}