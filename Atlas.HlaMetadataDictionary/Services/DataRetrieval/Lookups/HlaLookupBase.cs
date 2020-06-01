using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Models.LookupEntities;
using Atlas.HlaMetadataDictionary.Repositories.LookupRepositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
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
        public abstract Task<IEnumerable<HlaLookupTableEntity>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion);

        protected async Task<HlaLookupTableEntity> GetHlaLookupTableEntityIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            var lookupResult = await GetLookupResultFromRepository(locus, lookupName, typingMethod, hlaNomenclatureVersion);
            return lookupResult ?? throw new InvalidHlaException(locus, lookupName);
        }

        protected async Task<HlaLookupTableEntity> TryGetHlaLookupTableEntity(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion
        )
        {
            return await GetLookupResultFromRepository(locus, lookupName, typingMethod, hlaNomenclatureVersion);
        }

        private async Task<HlaLookupTableEntity> GetLookupResultFromRepository(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            return await hlaLookupRepository.GetHlaLookupTableEntityIfExists(locus, lookupName, typingMethod, hlaNomenclatureVersion);
        }
    }
}