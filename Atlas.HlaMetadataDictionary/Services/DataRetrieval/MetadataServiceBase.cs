using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.InternalExceptions;
using LazyCache;
using System;
using System.Runtime.ExceptionServices;
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

        /// <summary>
        /// <see cref="GetMetadata"/>, for a caller that treats "this HLA name has no data" as an answer rather than as
        /// an error
        /// </summary>
        /// <returns>
        /// <c>(true, value)</c>, or <c>(false, default)</c> where the name has no data - which includes a name this
        /// service does not consider a valid lookup name, because to a caller converting HLA the two are the same
        /// outcome, and because <see cref="GetMetadata"/> has always made them the same outcome by wrapping both.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Additive, and <see cref="GetMetadata"/> is untouched: the throwing path is load-bearing for
        /// <c>DonorHlaExpander</c>, <c>SearchRunner</c> and <c>RepeatSearchRunner</c>, which use
        /// <see cref="HlaMetadataDictionaryException"/> as an expected-error pathway.
        /// </para>
        /// <para>
        /// <b>An infrastructure fault still throws</b>, and that is the point of having this as well as
        /// <see cref="GetMetadata"/>. Today a failed storage request is wrapped into the same exception as a missing
        /// name, so a caller that swallows lookup failures - <c>HlaConverterBase</c> does exactly that - silently treats
        /// a transient blip as "this HLA does not exist" and predicts from an incomplete expansion. Here the two are
        /// distinguishable for the first time: not-found comes back as <c>false</c>, and anything else propagates.
        /// </para>
        /// </remarks>
        protected async Task<(bool WasFound, T Value)> TryGetMetadata(Locus locus, string rawLookupName, string hlaNomenclatureVersion)
        {
            if (rawLookupName == newAllele)
            {
                return (true, default);
            }

            var formattedLookupName = FormatLookupName(rawLookupName);
            var key = BuildCacheKey(locus, formattedLookupName, hlaNomenclatureVersion);

            var cachedOutcome = cache.Get<LookupOutcome>(key);
            if (cachedOutcome != null)
            {
                return cachedOutcome.ValueOrNotFound();
            }

            if (!LookupNameIsValid(formattedLookupName))
            {
                return (false, default);
            }

            var outcome = await cache.GetOrAddAsync(
                key, () => Lookup(locus, formattedLookupName, hlaNomenclatureVersion), GetMemoryCacheOptions());

            return outcome.ValueOrNotFound();
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

            // The cached item is an OUTCOME, not a T, and that one change fixes two things the shipped
            // `cache.Get<T>(key) != null` test could not express.
            //
            // 1. A name that is not in the data is now remembered. PerformLookup throws for it, LazyCache removes an
            //    entry whose factory threw, and so the next donor carrying the same bad name repeated the whole thing:
            //    the storage request, two throws, the logged event and - because the HF sets are at nomenclature 3480
            //    while the refresh runs 3650 - the retry at the other version. 
            // 2. A lookup that legitimately returns null is now served from the cache. `cache.Get<T>,` cannot tell "not
            //    cached" from "cached, and the answer is null", so those were re-fetched forever.
            var cachedOutcome = cache.Get<LookupOutcome>(key);
            if (cachedOutcome != null)
            {
                return cachedOutcome.ValueOrThrow();
            }

            if (!LookupNameIsValid(formattedLookupName))
            {
                throw new ArgumentException($"{formattedLookupName} at locus {locus} is not a valid lookup name.");
            }

            var outcome = await cache.GetOrAddAsync(
                key, () => Lookup(locus, formattedLookupName, hlaNomenclatureVersion), GetMemoryCacheOptions());

            return outcome.ValueOrThrow();
        }

        /// <summary>
        /// <see cref="PerformLookup"/>, with "this HLA name has no data" turned into a value the cache can hold.
        /// </summary>
        /// <remarks>
        /// The boundary is <see cref="HlaMetadataDictionaryException"/> and its subclasses - which is what
        /// this dictionary throws to say a name is not in the data, <see cref="InvalidHlaException"/> included. Anything
        /// else is an infrastructure fault: a storage request that failed, a timeout, a serialisation error. Those are
        /// deliberately left to propagate out of the factory, so <c>LazyCache</c> evicts the entry and the next caller
        /// retries - caching a transient fault for the cache lifetime would turn a blip into an outage.
        ///
        /// <para>
        /// The exception that finally escapes <see cref="GetMetadata"/> is unchanged, deliberately: <c>DonorHlaExpander</c> passes
        /// <see cref="HlaMetadataDictionaryException"/> as a generic type argument to <c>ProcessBatchAsyncWithAnticipatedExceptions</c> -
        /// it IS the per-donor skip mechanism - and <c>SearchRunner</c> and <c>RepeatSearchRunner</c> use it as an expected-error pathway that
        /// completes the message rather than dead-lettering it. Narrowing what escapes would silently turn a skipped
        /// donor into a failed batch, so it is a separate change with its own evidence.
        /// </para>
        /// </remarks>
        private async Task<LookupOutcome> Lookup(Locus locus, string lookupName, string hlaNomenclatureVersion)
        {
            try
            {
                return LookupOutcome.Found(await PerformLookup(locus, lookupName, hlaNomenclatureVersion));
            }
            catch (HlaMetadataDictionaryException nameNotFound)
            {
                return LookupOutcome.NotFound(nameNotFound);
            }
        }

        /// <summary>What a lookup produced: a value, or the reason the name has no value. Both are cacheable.</summary>
        private sealed class LookupOutcome
        {
            private readonly T value;
            private readonly ExceptionDispatchInfo notFound;

            private LookupOutcome(T value, ExceptionDispatchInfo notFound)
            {
                this.value = value;
                this.notFound = notFound;
            }

            public static LookupOutcome Found(T value) => new(value, null);

            public static LookupOutcome NotFound(HlaMetadataDictionaryException exception) =>
                new(default, ExceptionDispatchInfo.Capture(exception));

            /// <summary>
            /// The value, or the original exception re-thrown. Captured rather than stored bare so that the first
            /// failure's stack survives every later re-throw of it, which a plain <c>throw stored;</c> would reset.
            /// </summary>
            public T ValueOrThrow()
            {
                notFound?.Throw();

                return value;
            }

            /// <summary>The same outcome, as data rather than as an exception. See <c>TryGetMetadata</c>.</summary>
            public (bool WasFound, T Value) ValueOrNotFound() => notFound == null ? (true, value) : (false, default);
        }

        protected virtual MemoryCacheEntryOptions GetMemoryCacheOptions() => new MemoryCacheEntryOptions {
            
            AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cache.DefaultCachePolicy.DefaultCacheDurationSeconds) 
        };

        private string BuildCacheKey(Locus locus, string lookupName, string hlaNomenclatureVersion)
            => $"{perTypeCacheKey}-{hlaNomenclatureVersion}-{locus}-{lookupName}";
    }
}