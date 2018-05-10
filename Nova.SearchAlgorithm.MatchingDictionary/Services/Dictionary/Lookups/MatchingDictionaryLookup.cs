using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Dictionary.Lookups
{
    public abstract class MatchingDictionaryLookup
    {
        private readonly IMatchedHlaRepository dictionaryRepository;

        protected MatchingDictionaryLookup(IMatchedHlaRepository dictionaryRepository)
        {
            this.dictionaryRepository = dictionaryRepository;
        }

        public abstract MatchingDictionaryEntry PerformLookup(string matchLocus, string lookupName);

        protected MatchingDictionaryEntry GetDictionaryEntry(string matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entry = dictionaryRepository.GetDictionaryEntry(matchLocus, lookupName, typingMethod);
            return entry ?? throw new InvalidHlaException(matchLocus, lookupName);
        }
    }
}
