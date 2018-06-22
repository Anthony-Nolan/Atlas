using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal abstract class MatchingDictionaryLookup
    {
        private readonly IMatchingDictionaryRepository dictionaryRepository;

        protected MatchingDictionaryLookup(IMatchingDictionaryRepository dictionaryRepository)
        {
            this.dictionaryRepository = dictionaryRepository;
        }

        public abstract Task<MatchingDictionaryEntry> PerformLookupAsync(MatchLocus matchLocus, string lookupName);

        protected async Task<MatchingDictionaryEntry> GetMatchingDictionaryEntryIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entry = await dictionaryRepository.GetMatchingDictionaryEntryIfExists(matchLocus, lookupName, typingMethod);
            return entry ?? throw new InvalidHlaException(matchLocus.ToString(), lookupName);
        }
    }
}
