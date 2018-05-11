using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    public class SerologyLookup : MatchingDictionaryLookup
    {
        public SerologyLookup(IMatchedHlaRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(string matchLocus, string lookupName)
        {
            return GetDictionaryEntry(matchLocus, lookupName, TypingMethod.Serology);
        }
    }
}
