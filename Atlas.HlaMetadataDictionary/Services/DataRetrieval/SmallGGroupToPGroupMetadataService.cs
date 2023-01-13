using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal interface ISmallGGroupToPGroupMetadataService
    {
        /// <summary>
        /// This is an optimised code path specifically for small g group to P-Group conversion,
        /// reliant on the fact that each small g Group will only have 0 or 1 corresponding P Groups.
        /// </summary>
        Task<string> ConvertSmallGGroupToPGroup(Locus locus, string smallGGroupLookupName, string hlaNomenclatureVersion);
    }

    internal class SmallGGroupToPGroupMetadataService : MetadataServiceBase<string>, ISmallGGroupToPGroupMetadataService
    {
        private const string CacheKey = nameof(SmallGGroupToPGroupMetadataService);

        private readonly ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository;

        public SmallGGroupToPGroupMetadataService(
            ISmallGGroupToPGroupMetadataRepository smallGGroupToPGroupMetadataRepository,
            IPersistentCacheProvider cacheProvider) 
            : base(CacheKey, cacheProvider)
        {
            this.smallGGroupToPGroupMetadataRepository = smallGGroupToPGroupMetadataRepository;
        }

        protected override bool LookupNameIsValid(string lookupName)
        {
            return !string.IsNullOrEmpty(lookupName);
        }

        protected override async Task<string> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var pGroupMetadata = await smallGGroupToPGroupMetadataRepository.GetPGroupBySmallGGroupIfExists(locus, lookupName, hlaNomenclatureVersion);

            if (pGroupMetadata == null)
            {
                throw new HlaMetadataDictionaryException(locus, lookupName, "Small g group not found; could not be converted to P group.");
            }

            return pGroupMetadata.PGroup.IsNullOrEmpty() ? null : pGroupMetadata.PGroup;
        }

        public async Task<string> ConvertSmallGGroupToPGroup(Locus locus, string smallGGroupLookupName, string hlaNomenclatureVersion)
        {
            if (smallGGroupLookupName == null)
            {
                return null;
            }

            return await GetMetadata(locus, smallGGroupLookupName, hlaNomenclatureVersion);
        }
    }
}