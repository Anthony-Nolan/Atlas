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
            var entry = await GetEntryFromMatchingDictionaryRepository(matchLocus, lookupName, typingMethod);
            return entry ?? throw new InvalidHlaException(matchLocus, lookupName);
        }

        protected bool TryGetMatchingDictionaryEntry(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod, out MatchingDictionaryEntry entry)
        {
            var task = Task.Run(() =>
                GetEntryFromMatchingDictionaryRepository(matchLocus, lookupName, typingMethod));
            // Note: use of Task.Result means that any exceptions raised will be wrapped in an AggregateException
            entry = task.Result;
            return entry != null;
        }

        private async Task<MatchingDictionaryEntry> GetEntryFromMatchingDictionaryRepository(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            return await dictionaryRepository.GetMatchingDictionaryEntryIfExists(matchLocus, lookupName, typingMethod);
        }
    }
}
