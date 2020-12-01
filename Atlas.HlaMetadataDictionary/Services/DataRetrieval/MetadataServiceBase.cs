using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using LazyCache;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.HlaMetadataDictionary.Services.DataRetrieval
{
    internal abstract class MetadataServiceBase<T>
    {
        private readonly string perTypeCacheKey;
        private readonly IAppCache cache;

        protected MetadataServiceBase(string perTypeCacheKey, IPersistentCacheProvider cacheProvider)
        {
            this.perTypeCacheKey = perTypeCacheKey;
            cache = cacheProvider.Cache;
        }

        protected async Task<T> GetMetadata(Locus locus, string rawLookupName, string hlaNomenclatureVersion)
        {
            try
            {
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
            // TODO: ATLAS-749: Find a less flakey way to do this. 
            return lookupName?.Trim().TrimStart('*').Split("[NULL-AS]").First();
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
            
            return await cache.GetOrAddAsync(key, () => PerformLookup(locus, formattedLookupName, hlaNomenclatureVersion));
        }

        private string BuildCacheKey(Locus locus, string lookupName, string hlaNomenclatureVersion)
            => $"{perTypeCacheKey}-{hlaNomenclatureVersion}-{locus}-{lookupName}";
    }
}