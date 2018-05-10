using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class DictionaryTableEntity : TableEntity
    {
        public string SerialisedDictionaryEntry { get; set; }

        public DictionaryTableEntity() { }

        public DictionaryTableEntity(string matchLocus, string lookupName, TypingMethod typingMethod) 
            : base(matchLocus, GetRowKey(lookupName, typingMethod))
        {
        }

        public static string GetRowKey(string lookupName, TypingMethod typingMethod)
        {
            return $"{lookupName}-{typingMethod.ToString()}";
        }
    }
}