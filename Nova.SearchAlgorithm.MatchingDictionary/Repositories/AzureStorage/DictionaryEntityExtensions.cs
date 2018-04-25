using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class DictionaryEntityExtensions
    {
        internal static DictionaryTableEntity ToTableEntity(this MatchingDictionaryEntry entry)
        {
            return new DictionaryTableEntity(entry.MatchLocus, entry.LookupName, entry.TypingMethod)
            {
                SerialisedDictionaryEntry = JsonConvert.SerializeObject(entry)
            };
        }

        internal static MatchingDictionaryEntry ToDictionaryEntry(this DictionaryTableEntity result)
        {
            return JsonConvert.DeserializeObject<MatchingDictionaryEntry>(result.SerialisedDictionaryEntry);
        }
    }
}