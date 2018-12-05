using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
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
        public abstract Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName);

        protected async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus, string lookupName, TypingMethod typingMethod)
        {
            var lookupResult = await GetLookupResultFromRepository(locus, lookupName, typingMethod);
            return lookupResult ?? throw new InvalidHlaException(locus, lookupName);
        }

        protected async Task<HlaLookupTableEntity> TryGetHlaLookupTableEntity(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod
        )
        {
            return await GetLookupResultFromRepository(locus, lookupName, typingMethod);
        }

        private async Task<HlaLookupTableEntity> GetLookupResultFromRepository(Locus locus, string lookupName, TypingMethod typingMethod)
        {
            return await hlaLookupRepository.GetHlaLookupTableEntityIfExists(locus, lookupName, typingMethod);
        }
    }
}