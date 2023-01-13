using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval.Lookups
{
    /// <summary>
    /// Base class for all lookups that involve searching the lookup repository
    /// with one or more allele lookup names.
    /// </summary>
    internal abstract class AlleleNamesLookupBase : HlaLookupBase
    {
        private readonly IAlleleNamesMetadataService alleleNamesMetadataService;

        protected AlleleNamesLookupBase(
            IHlaMetadataRepository hlaMetadataRepository,
            IAlleleNamesMetadataService alleleNamesMetadataService)
            : base(hlaMetadataRepository)
        {
            this.alleleNamesMetadataService = alleleNamesMetadataService;
        }

        public override async Task<List<HlaMetadataTableRow>> PerformLookupAsync(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var alleleNamesToLookup = await GetAlleleLookupNames(locus, lookupName, hlaNomenclatureVersion);
            return await GetHlaMetadataRows(locus, alleleNamesToLookup, hlaNomenclatureVersion);
        }

        protected abstract Task<List<string>> GetAlleleLookupNames(Locus locus, string lookupName, string hlaNomenclatureVersion);

        private async Task<List<HlaMetadataTableRow>> GetHlaMetadataRows(
            Locus locus,
            IEnumerable<string> alleleNamesToLookup,
            string hlaNomenclatureVersion
        )
        {
            var lookupTasks = alleleNamesToLookup.Select(name => GetHlaMetadataRowsForAlleleNameIfExists(locus, name, hlaNomenclatureVersion)).ToList();
            var metadataRows = await Task.WhenAll(lookupTasks);

            return metadataRows.SelectMany(rows => rows).ToList();
        }

        /// <summary>
        /// Query matching lookup repository using the allele lookup name.
        /// If nothing is found, try again using the current version(s) of the allele name.
        /// Else an invalid HLA exception will be thrown.
        /// </summary>
        private async Task<IList<HlaMetadataTableRow>> GetHlaMetadataRowsForAlleleNameIfExists(
            Locus locus,
            string lookupName,
            string hlaNomenclatureVersion)
        {
            var metadataRow = await TryGetHlaMetadataRowByAlleleLookupName(locus, lookupName, hlaNomenclatureVersion);
            if (metadataRow != null)
            {
                return new List<HlaMetadataTableRow> {metadataRow};
            }

            return await GetHlaMetadataRowsByCurrentAlleleNamesIfExists(locus, lookupName, hlaNomenclatureVersion);
        }

        private async Task<HlaMetadataTableRow> TryGetHlaMetadataRowByAlleleLookupName(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            return await TryGetHlaMetadataRow(locus, lookupName, TypingMethod.Molecular, hlaNomenclatureVersion);
        }

        private async Task<HlaMetadataTableRow[]> GetHlaMetadataRowsByCurrentAlleleNamesIfExists(
            Locus locus,
            string lookupName,
            string hlaNomenclatureVersion
        )
        {
            var currentNames = await alleleNamesMetadataService.GetCurrentAlleleNames(locus, lookupName, hlaNomenclatureVersion);
            var lookupTasks = currentNames.Select(name => GetHlaMetadataRowIfExists(locus, name, TypingMethod.Molecular, hlaNomenclatureVersion)).ToList();
            return await Task.WhenAll(lookupTasks);
        }
    }
}