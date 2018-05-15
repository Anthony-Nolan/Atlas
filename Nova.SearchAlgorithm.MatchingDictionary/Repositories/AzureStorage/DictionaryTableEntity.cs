using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class DictionaryTableEntity : TableEntity
    {
        public string SerialisedDictionaryEntry { get; set; }

        public DictionaryTableEntity() { }

        public DictionaryTableEntity(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod) 
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