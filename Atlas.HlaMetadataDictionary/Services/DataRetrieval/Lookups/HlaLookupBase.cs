using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.Lookups
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
        public abstract Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName, string hlaDatabaseVersion);

        protected async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaDatabaseVersion)
        {
            var lookupResult = await GetLookupResultFromRepository(locus, lookupName, typingMethod, hlaDatabaseVersion);
            return lookupResult ?? throw new InvalidHlaException(new HlaInfo(locus, lookupName));
        }

        protected async Task<HlaLookupTableEntity> TryGetHlaLookupTableEntity(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaDatabaseVersion
        )
        {
            return await GetLookupResultFromRepository(locus, lookupName, typingMethod, hlaDatabaseVersion);
        }

        private async Task<HlaLookupTableEntity> GetLookupResultFromRepository(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaDatabaseVersion)
        {
            return await hlaLookupRepository.GetHlaLookupTableEntityIfExists(locus, lookupName, typingMethod, hlaDatabaseVersion);
        }
    }
}