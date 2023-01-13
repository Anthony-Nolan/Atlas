using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.AzureStorage;

namespace Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories
{
    internal interface IAlleleNamesMetadataRepository : IHlaMetadataRepository
    {
        Task<IAlleleNameMetadata> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion);
    }

    internal class AlleleNamesMetadataRepository :
        HlaMetadataRepositoryBase,
        IAlleleNamesMetadataRepository
    {
        private const string DataTableReferencePrefix = "AlleleNamesData";
        private const string CacheKeyAlleleNames = "AlleleNames";

        public AlleleNamesMetadataRepository(
            ICloudTableFactory factory,
            ITableReferenceRepository tableReferenceRepository,
            IPersistentCacheProvider cacheProvider,
            ILogger logger)
            : base(factory, tableReferenceRepository, DataTableReferencePrefix, cacheProvider, CacheKeyAlleleNames, logger)
        {
        }

        public async Task<IAlleleNameMetadata> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var row = await GetHlaMetadataRowIfExists(locus, lookupName, TypingMethod.Molecular, hlaNomenclatureVersion);

            return row == null
                ? null
                : new AlleleNameMetadata(row.Locus, row.LookupName, row.GetHlaInfo<List<string>>());
        }
    }
}
