using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface IGGroupToPGroupMetadataService
    {
        /// <summary>
        /// This is an optimised code path specifically for G-Group to P-Group conversion,
        /// reliant on the fact that each G Group will only have 1 or 0 corresponding P Groups.
        /// </summary>
        Task<string> ConvertGGroupToPGroup(Locus locus, string gGroupLookupName, string hlaNomenclatureVersion);
    }

    internal class GGroupToPGroupMetadataService : MetadataServiceBase<string>, IGGroupToPGroupMetadataService
    {
        private const string CacheKey = nameof(GGroupToPGroupMetadataService);

        private readonly IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository;

        public GGroupToPGroupMetadataService(
            IGGroupToPGroupMetadataRepository gGroupToPGroupMetadataRepository,
            IPersistentCacheProvider cacheProvider) 
            : base(CacheKey, cacheProvider)
        {
            this.gGroupToPGroupMetadataRepository = gGroupToPGroupMetadataRepository;
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return true;
        }

        protected override async Task<string> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var pGroupMetadata = await gGroupToPGroupMetadataRepository.GetPGroupByGGroupIfExists(locus, lookupName, hlaNomenclatureVersion);

            if (pGroupMetadata == null)
            {
                throw new HlaMetadataDictionaryException(locus, lookupName, "GGroup not recognised, could not be converted to PGroup.");
            }

            return pGroupMetadata.PGroup.IsNullOrEmpty() ? null : pGroupMetadata.PGroup;
        }

        public async Task<string> ConvertGGroupToPGroup(Locus locus, string gGroupLookupName, string hlaNomenclatureVersion)
        {
            if (gGroupLookupName == null)
            {
                return null;
            }

            return await GetMetadata(locus, gGroupLookupName, hlaNomenclatureVersion);
        }
    }
}
