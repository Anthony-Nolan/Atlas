using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring
{
    /// <summary>
    /// Wraps a cache provided by LazyCache
    /// Used to ensure consistent key creation for cached items
    /// </summary>
    public interface IScoringCache
    {
        MatchGrade GetOrAddMatchGrade(Locus locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchGrade> func);
        MatchConfidence GetOrAddMatchConfidence(Locus? locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchConfidence> func);
        bool? GetOrAddIsAntigenMatch(Locus? locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, bool?> func);
    }

    public class ScoringCache : IScoringCache
    {
        private readonly IAppCache cache;
        private readonly IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor;

        public ScoringCache(
            IPersistentCacheProvider cacheProvider,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor)
        {
            cache = cacheProvider.Cache;
            this.hlaNomenclatureVersionAccessor = hlaNomenclatureVersionAccessor;
        }

        public MatchGrade GetOrAddMatchGrade(Locus locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchGrade> func)
        {
            var cacheKey = $"MatchGrade:v{hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion()};l{locus};d{donorHlaName};p{patientHlaName}";
            return cache.GetOrAdd(cacheKey, func);
        }

        public MatchConfidence GetOrAddMatchConfidence(
            Locus? locus,
            string patientHlaName,
            string donorHlaName,
            Func<ICacheEntry, MatchConfidence> func)
        {
            var cacheKey = $"MatchConfidence:v{hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion()};l{locus};d{donorHlaName};p{patientHlaName}";
            return cache.GetOrAdd(cacheKey, func);
        }

        /// <inheritdoc />
        public bool? GetOrAddIsAntigenMatch(
            Locus? locus, 
            string patientHlaName, 
            string donorHlaName, 
            Func<ICacheEntry, bool?> func)
        {
            var cacheKey = $"IsAntigenMatch:v{hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion()};l{locus};d{donorHlaName};p{patientHlaName}";
            return cache.GetOrAdd(cacheKey, func);
        }
    }
}