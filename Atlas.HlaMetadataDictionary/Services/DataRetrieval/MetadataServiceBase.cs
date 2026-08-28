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

        /// <remarks>
        /// <see cref="HlaMetadataDictionaryException"/> means one thing and one thing only: <b>this HLA name has no
        /// data</b>. Only that is caught and re-thrown with the name the caller actually asked about - a nested lookup
        /// reports the expanded allele it failed on, which is rarely the name the caller would recognise.
        ///
        /// <para>
        /// Everything else leaves as itself. This used to be a <c>catch (Exception)</c>, which re-labelled a failed
        /// storage request as a missing name, and there was then no way for anyone above to tell the two apart: the
        /// matching algorithm skipped a donor that a blip had hidden, and Match Prediction predicted from an
        /// incomplete expansion. A <c>RequestFailedException</c> now arrives as a <c>RequestFailedException</c>, which
        /// is also what the type-keyed Polly policies used elsewhere in this codebase expect to see.
        /// </para>
        /// </remarks>
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
            catch (HlaMetadataDictionaryException nameNotFound)
            {
                var msg = $"Failed to lookup '{rawLookupName}' at locus {locus}.";
                throw new HlaMetadataDictionaryException(locus, rawLookupName, msg, nameNotFound);
            }
        }

        /// <summary>
        /// <see cref="GetMetadata"/>, for a caller that treats "this HLA name has no data" as an answer rather than as
        /// an error
        /// </summary>
        /// <returns>
        /// <c>(true, value)</c>, or <c>(false, default)</c> where the name has no data - which includes a name this
        /// service does not consider a valid lookup name, because to a caller converting HLA the two are the same
        /// outcome.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Additive: the throwing path is load-bearing for <c>DonorHlaExpander</c>, <c>SearchRunner</c> and
        /// <c>RepeatSearchRunner</c>, which use <see cref="HlaMetadataDictionaryException"/> as an expected-error
        /// pathway. Both routes read one cached outcome, so whichever caller meets a bad name first pays for it.
        /// </para>
        /// <para>
        /// <b>An infrastructure fault throws</b>, and that is the point of having this as well as
        /// <see cref="GetMetadata"/>. Not-found comes back as <c>false</c>; anything else propagates as itself.
        /// </para>
        /// </remarks>
        protected async Task<(bool WasFound, T Value)> TryGetMetadata(Locus locus, string rawLookupName, string hlaNomenclatureVersion)
        {
            if (rawLookupName == newAllele)
            {
                return (true, default);
            }

            var outcome = await GetOrAddCachedOutcome(locus, FormatLookupName(rawLookupName), hlaNomenclatureVersion);

            return outcome?.ValueOrNotFound() ?? (false, default);
        }

        protected abstract bool LookupNameIsValid(string lookupName);

        protected abstract Task<T> PerformLookup(Locus locus, string lookupName, string hlaNomenclatureVersion);

        private static string FormatLookupName(string lookupName)
        {
            // Nothing to format, and nothing below here survives a null. This used to be moot: the formatting threw a
            // NullReferenceException and GetMetadata re-labelled it as a lookup failure along with everything else.
            // Now only genuine misses are re-labelled, so an untyped locus has to reach LookupNameIsValid and be
            // reported as the missing name it is - the same answer an empty string already gets.
            if (string.IsNullOrWhiteSpace(lookupName))
            {
                return lookupName;
            }

            var lookupNameWithoutAsterisk = AlleleSplitter.RemovePrefix(lookupName.Trim());
            return NullAlleleHandling.GetOriginalAlleleFromCombinedName(lookupNameWithoutAsterisk);
        }

        private async Task<T> GetOrAddCachedMetadata(Locus locus, string formattedLookupName, string hlaNomenclatureVersion)
        {
            var outcome = await GetOrAddCachedOutcome(locus, formattedLookupName, hlaNomenclatureVersion);

            // Not-found, not a programming fault: the same case TryGetMetadata reports as `(false, default)`, and the
            // two routes have to agree on it. It was an ArgumentException, which only reached callers as a missing
            // name because GetMetadata used to re-label everything.
            return outcome == null
                ? throw new InvalidHlaException(locus, formattedLookupName)
                : outcome.ValueOrThrow();
        }

        /// <summary>
        /// The one cache path, read two ways. <see cref="GetMetadata"/> and <see cref="TryGetMetadata"/> differ only in
        /// how they report a name with no data, so they must not differ in how they cache one - a caching change made
        /// in one of two near-identical copies is how the throwing and non-throwing routes would drift apart.
        /// </summary>
        /// <returns>
        /// The outcome, or <c>null</c> where this service does not consider the name a valid lookup name at all -
        /// which each caller reports in its own way. Unambiguous, because <see cref="Lookup"/> never returns null.
        /// </returns>
        /// <remarks>
        /// The cached item is an OUTCOME, not a T, and that one change fixes two things the shipped
        /// `cache.Get&lt;T&gt;(key) != null` test could not express.
        ///
        /// <para>
        /// 1. A name that is not in the data is now remembered. PerformLookup throws for it, LazyCache removes an
        /// entry whose factory threw, and so the next donor carrying the same bad name repeated the whole thing: the
        /// storage request, two throws, the logged event and - because the HF sets are at nomenclature 3480 while the
        /// refresh runs 3650 - the retry at the other version.
        /// </para>
        /// <para>
        /// 2. A lookup that legitimately returns null is now served from the cache. `cache.Get&lt;T&gt;` cannot tell
        /// "not cached" from "cached, and the answer is null", so those were re-fetched forever.
        /// </para>
        /// </remarks>
        private async Task<LookupOutcome> GetOrAddCachedOutcome(Locus locus, string formattedLookupName, string hlaNomenclatureVersion)
        {
            var key = BuildCacheKey(locus, formattedLookupName, hlaNomenclatureVersion);

            var cachedOutcome = cache.Get<LookupOutcome>(key);
            if (cachedOutcome != null)
            {
                return cachedOutcome;
            }

            return LookupNameIsValid(formattedLookupName)
                ? await cache.GetOrAddAsync(key, () => Lookup(locus, formattedLookupName, hlaNomenclatureVersion), GetMemoryCacheOptions())
                : null;
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
        /// That boundary only holds because <see cref="GetMetadata"/> no longer re-labels every exception as an
        /// <see cref="HlaMetadataDictionaryException"/>. While it did, a nested lookup - <c>AlleleNamesLookupBase</c>
        /// and <c>AlleleGroupLookup</c> both make one - handed this catch a wrapped storage fault that was
        /// indistinguishable from a missing name, and it was then cached as one for the lifetime of the persistent
        /// cache. Every site that means "no data" therefore has to say so with this exception and nothing else; see
        /// <c>MacLookup</c> and <c>SearchRelatedMetadataServiceBase.GetHlaLookup</c> for the two that had to be
        /// brought into line.
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
            /// The value, or the original exception re-thrown. Captured rather than stored bare so that each re-throw
            /// reproduces the first failure's stack, which a plain <c>throw stored;</c> would reset. This is what
            /// <c>System.Lazy&lt;T&gt;</c> does with a cached failure, and is safe for the same reason: the runtime
            /// freezes the captured frames and clones them per thread.
            /// </summary>
            /// <remarks>
            /// The cached exception INSTANCE is shared, though, so read a stack trace from the exception you caught,
            /// not from the inner exception of one later. Under concurrent re-throws of the same cached outcome the
            /// inner frames are not attributable; the type, the message and each caller's own wrapper are.
            /// </remarks>
            public T ValueOrThrow()
            {
                notFound?.Throw();

                return value;
            }

            /// <summary>The same outcome, as data rather than as an exception. See <c>TryGetMetadata</c>.</summary>
            public (bool WasFound, T Value) ValueOrNotFound() => notFound == null ? (true, value) : (false, default);
        }

        protected virtual MemoryCacheEntryOptions GetMemoryCacheOptions() => new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(cache.DefaultCachePolicy.DefaultCacheDurationSeconds)
        };

        private string BuildCacheKey(Locus locus, string lookupName, string hlaNomenclatureVersion)
            => $"{perTypeCacheKey}-{hlaNomenclatureVersion}-{locus}-{lookupName}";
    }
}