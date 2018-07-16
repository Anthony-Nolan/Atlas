using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal abstract class HlaMatchingLookupBase
    {
        private readonly IHlaMatchingLookupRepository hlaMatchingLookupRepository;

        protected HlaMatchingLookupBase(IHlaMatchingLookupRepository hlaMatchingLookupRepository)
        {
            this.hlaMatchingLookupRepository = hlaMatchingLookupRepository;
        }

        public abstract Task<HlaMatchingLookupResult> PerformLookupAsync(MatchLocus matchLocus, string lookupName);

        protected async Task<HlaMatchingLookupResult> GetHlaMatchingLookupResultIfExists(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var lookupResult = await GetLookupResultFromRepository(matchLocus, lookupName, typingMethod);
            return lookupResult ?? throw new InvalidHlaException(matchLocus, lookupName);
        }

        protected bool TryGetHlaMatchingLookupResult(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod, out HlaMatchingLookupResult lookupResult)
        {
            var task = Task.Run(() =>
                GetLookupResultFromRepository(matchLocus, lookupName, typingMethod));
            // Note: use of Task.Result means that any exceptions raised will be wrapped in an AggregateException
            lookupResult = task.Result;
            return lookupResult != null;
        }

        private async Task<HlaMatchingLookupResult> GetLookupResultFromRepository(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            return await hlaMatchingLookupRepository.GetHlaMatchLookupResultIfExists(matchLocus, lookupName, typingMethod);
        }
    }
}
