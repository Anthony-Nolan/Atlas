using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using LazyCache;
using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.Hla.Services.AlleleNameUtils;
using Atlas.Common.Public.Models.GeneticData;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal abstract class MetadataServiceBase<T>
    {
        private readonly string perTypeCacheKey;
        private readonly IAppCache cache;
        private const string newAllele = "NEW";

        protected MetadataServiceBase(string perTypeCacheKey, IPersistentCacheProvider cacheProvider)
        {
            this.perTypeCacheKey = perTypeCacheKey;
            cache = cacheProvider.Cache;
        }

        protected async Task<T> GetMetadata(Locus locus, string rawLookupName, string hlaNomenclatureVersion)
        {
            try
            {
                if (rawLookupName == newAllele)
                {
                    return await Task.FromResult(default(T));
                }

                var formattedLookupName = FormatLookupName(rawLookupName);
                return await GetOrAddCachedMetadata(locus, formattedLookupName, hlaNomenclatureVersion);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to lookup '{rawLookupName}' at locus {locus}.";
                throw new HlaMetadataDictionaryException(locus, rawLookupName, msg, ex);
            }
        }

        protected abstract bool LookupNameIsValid(string lookupName);

        protected abstract Task<T> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion);

        private static string FormatLookupName(string lookupName)
        {
            var lookupNameWithoutAsterisk = AlleleSplitter.RemovePrefix(lookupName?.Trim());
            return NullAlleleHandling.GetOriginalAlleleFromCombinedName(lookupNameWithoutAsterisk);
        }

        private async Task<T> GetOrAddCachedMetadata(Locus locus, string formattedLookupName, string hlaNomenclatureVersion)
        {
            var key = BuildCacheKey(locus, formattedLookupName, hlaNomenclatureVersion);
            var existingRecord = cache.Get<T>(key);
            if (existingRecord != null)
            {
                return existingRecord;
            }

            if (!LookupNameIsValid(formattedLookupName))
            {
                throw new ArgumentException($"{formattedLookupName} at locus {locus} is not a valid lookup name.");
            }
            
            return await cache.GetOrAddAsync(key, () => PerformLookup(locus, formattedLookupName, hlaNomenclatureVersion), GetMemoryCacheOptions());
        }

        protected virtual MemoryCacheEntryOptions GetMemoryCacheOptions() => new MemoryCacheEntryOptions {
            
            AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cache.DefaultCachePolicy.DefaultCacheDurationSeconds) 
        };

        private string BuildCacheKey(Locus locus, string lookupName, string hlaNomenclatureVersion)
            => $"{perTypeCacheKey}-{hlaNomenclatureVersion}-{locus}-{lookupName}";
    }
}