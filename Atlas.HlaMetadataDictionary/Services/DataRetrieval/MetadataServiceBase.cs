using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using LazyCache;
using System;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal abstract class MetadataServiceBase<T>
    {
        private readonly string cacheKey;
        private readonly IAppCache cache;

        protected MetadataServiceBase(string cacheKey, IPersistentCacheProvider cacheProvider)
        {
            this.cacheKey = cacheKey;
            cache = cacheProvider.Cache;
        }

        protected async Task<T> GetMetadata(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            try
            {
                if (!LookupNameIsValid(lookupName))
                {
                    throw new ArgumentException($"{lookupName} at locus {locus} is not a valid lookup name.");
                }

                var formattedLookupName = FormatLookupName(lookupName);

                return await GetOrAddCachedMetadata(locus, formattedLookupName, hlaNomenclatureVersion);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to lookup '{lookupName}' at locus {locus}.";
                throw new HlaMetadataDictionaryException(locus, lookupName, msg, ex);
            }
        }

        protected abstract bool LookupNameIsValid(string lookupName);

        protected abstract Task<T> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion);

        private static string FormatLookupName(string lookupName)
        {
            return lookupName.Trim().TrimStart('*');
        }

        private async Task<T> GetOrAddCachedMetadata(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            var key = BuildCacheKey(locus, lookupName, hlaNomenclatureVersion);
            return await cache.GetOrAddAsync(key, () => PerformLookup(locus, lookupName, hlaNomenclatureVersion));
        }

        private string BuildCacheKey(Locus locus, string lookupName, string hlaNomenclatureVersion) => $"{cacheKey}-{hlaNomenclatureVersion}-{locus}-{lookupName}";
    }
}