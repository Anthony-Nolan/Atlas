using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    internal abstract class HlaLookupBase
    {
        private readonly IHlaMetadataRepository hlaMetadataRepository;

        protected HlaLookupBase(IHlaMetadataRepository hlaMetadataRepository)
        {
            this.hlaMetadataRepository = hlaMetadataRepository;
        }

        /// <summary>
        /// Lookup the submitted HLA details.
        /// </summary>
        /// <exception cref="InvalidHlaException">Thrown if no lookup results found.</exception>
        public abstract Task<List<HlaMetadataTableRow>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion);

        protected async Task<HlaMetadataTableRow> GetHlaMetadataRowIfExists(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            var metadataRow = await GetMetadataRowFromRepository(locus, lookupName, typingMethod, hlaNomenclatureVersion);
            return metadataRow ?? throw new InvalidHlaException(locus, lookupName);
        }

        protected async Task<HlaMetadataTableRow> TryGetHlaMetadataRow(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion
        )
        {
            return await GetMetadataRowFromRepository(locus, lookupName, typingMethod, hlaNomenclatureVersion);
        }

        private async Task<HlaMetadataTableRow> GetMetadataRowFromRepository(
            Locus locus,
            string lookupName,
            TypingMethod typingMethod,
            string hlaNomenclatureVersion)
        {
            return await hlaMetadataRepository.GetHlaMetadataRowIfExists(locus, lookupName, typingMethod, hlaNomenclatureVersion);
        }
    }
}