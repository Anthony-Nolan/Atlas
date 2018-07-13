using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    public class HlaLookupTableEntity : TableEntity
    {
        public MatchLocus MatchLocus { get; set; }
        public TypingMethod TypingMethod { get; set; }
        public string LookupName { get; set; }
        public string SerialisedHlaInfo { get; set; }

        public HlaLookupTableEntity() { }

        public HlaLookupTableEntity(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod) 
            : base(GetPartition(matchLocus), GetRowKey(lookupName, typingMethod))
        {
            MatchLocus = matchLocus;
            TypingMethod = typingMethod;
            LookupName = lookupName;
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