using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class MatchingDictionaryTableEntity : TableEntity
    {
        public string SerialisedMatchingDictionaryEntry { get; set; }

        public MatchingDictionaryTableEntity() { }

        public MatchingDictionaryTableEntity(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod) 
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