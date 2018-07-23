using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.MatchingDictionary.Services.Lookups
{
    internal abstract class HlaLookupBase
    {
        private readonly IHlaLookupRepository hlaLookupRepository;

        protected HlaLookupBase(IHlaLookupRepository hlaLookupRepository)
        {
            this.hlaLookupRepository = hlaLookupRepository;
        }

        /// <summary>
        /// Lookup the submitted HLA details.
        /// </summary>
        /// <exception cref="InvalidHlaException">Thrown if no lookup results found.</exception>
        public abstract Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(MatchLocus matchLocus, string lookupName);

        protected async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            var lookupResult = await GetLookupResultFromRepository(matchLocus, lookupName, typingMethod);
            return lookupResult ?? throw new InvalidHlaException(matchLocus, lookupName);
        }

        protected bool TryGetHlaLookupTableEntity(
            MatchLocus matchLocus, string lookupName, TypingMethod typingMethod, out HlaLookupTableEntity lookupResult)
        {
            var task = Task.Run(() =>
                GetLookupResultFromRepository(matchLocus, lookupName, typingMethod));
            // Note: use of Task.Result means that any exceptions raised will be wrapped in an AggregateException
            lookupResult = task.Result;
            return lookupResult != null;
        }

        private async Task<HlaLookupTableEntity> GetLookupResultFromRepository(MatchLocus matchLocus, string lookupName, TypingMethod typingMethod)
        {
            return await hlaLookupRepository.GetHlaLookupTableEntityIfExists(matchLocus, lookupName, typingMethod);
        }
    }
}
