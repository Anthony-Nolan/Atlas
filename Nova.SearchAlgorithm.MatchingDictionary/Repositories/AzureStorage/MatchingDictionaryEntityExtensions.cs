using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class MatchingDictionaryEntityExtensions
    {
        internal static MatchingDictionaryTableEntity ToTableEntity(this MatchingDictionaryEntry entry)
        {
            return new MatchingDictionaryTableEntity(entry.MatchLocus, entry.LookupName, entry.TypingMethod)
            {
                SerialisedMatchingDictionaryEntry = JsonConvert.SerializeObject(entry)
            };
        }

        internal static MatchingDictionaryEntry ToMatchingDictionaryEntry(this MatchingDictionaryTableEntity result)
        {
            return JsonConvert.DeserializeObject<MatchingDictionaryEntry>(result.SerialisedMatchingDictionaryEntry);
        }
    }
}