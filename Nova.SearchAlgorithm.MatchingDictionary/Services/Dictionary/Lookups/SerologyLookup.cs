using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    internal class SerologyLookup : MatchingDictionaryLookup
    {
        public SerologyLookup(IMatchingDictionaryRepository dictionaryRepository) : base(dictionaryRepository)
        {
        }

        public override Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName)
        {
            return GetDictionaryEntry(matchLocus, lookupName, TypingMethod.Serology);
        }
    }
}
