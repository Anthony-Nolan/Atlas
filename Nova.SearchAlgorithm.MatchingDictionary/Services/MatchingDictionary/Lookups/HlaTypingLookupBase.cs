using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.MatchingDictionary.Lookups
{
    internal abstract class HlaTypingLookupBase
    {
        private readonly IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository;

        protected HlaTypingLookupBase(IPreCalculatedHlaMatchRepository preCalculatedHlaMatchRepository)
        {
            this.preCalculatedHlaMatchRepository = preCalculatedHlaMatchRepository;
        }

        public abstract Task<PreCalculatedHlaMatchInfo> PerformLookupAsync(MatchLocus matchLocus, string lookupName);

        protected async Task<PreCalculatedHlaMatchInfo> GetPreCalculatedHlaMatchInfoIfExists(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var entry = await GetEntryFromMatchingDictionaryRepository(matchLocus, lookupName, typingMethod);
            return entry ?? throw new InvalidHlaException(matchLocus, lookupName);
        }

        protected bool TryGetPreCalculatedHlaMatchInfo(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod, out PreCalculatedHlaMatchInfo entry)
        {
            var task = Task.Run(() =>
                GetEntryFromMatchingDictionaryRepository(matchLocus, lookupName, typingMethod));
            // Note: use of Task.Result means that any exceptions raised will be wrapped in an AggregateException
            entry = task.Result;
            return entry != null;
        }

        private async Task<PreCalculatedHlaMatchInfo> GetEntryFromMatchingDictionaryRepository(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            return await preCalculatedHlaMatchRepository.GetPreCalculatedHlaMatchInfoIfExists(matchLocus, lookupName, typingMethod);
        }
    }
}
