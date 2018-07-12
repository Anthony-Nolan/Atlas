using Newtonsoft.Json;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;

namespace Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage
{
    internal static class MatchingDictionaryEntityExtensions
    {
        internal static MatchingDictionaryTableEntity ToTableEntity(this PreCalculatedHlaMatchInfo entry)
        {
            return new MatchingDictionaryTableEntity(entry.MatchLocus, entry.LookupName, entry.TypingMethod)
            {
                SerialisedPreCalculatedHlaMatchInfo = JsonConvert.SerializeObject(entry)
            };
        }

        internal static PreCalculatedHlaMatchInfo ToPreCalculatedHlaMatchInfo(this MatchingDictionaryTableEntity result)
        {
            return JsonConvert.DeserializeObject<PreCalculatedHlaMatchInfo>(result.SerialisedPreCalculatedHlaMatchInfo);
        }
    }
}