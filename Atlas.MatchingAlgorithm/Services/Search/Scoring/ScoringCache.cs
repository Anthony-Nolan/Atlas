using System;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading
{
    /// <summary>
    /// Wraps a cache provided by LazyCache
    /// Used to ensure consistent key creation for cached items
    /// </summary>
    public interface IScoringCache
    {
        MatchGrade GetOrAddMatchGrade(Locus locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchGrade> func);
        MatchConfidence GetOrAddMatchConfidence(Locus? locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchConfidence> func);
    }

    public class ScoringCache : IScoringCache
    {
        private readonly IAppCache cache;
        private readonly IActiveHlaVersionAccessor hlaVersionProvider;

        public ScoringCache(
            IPersistentCacheProvider cacheProvider,
            IActiveHlaVersionAccessor hlaVersionProvider)
        {
            this.cache = cacheProvider.Cache;
            this.hlaVersionProvider = hlaVersionProvider;
        }

        public MatchGrade GetOrAddMatchGrade(Locus locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchGrade> func)
        {
            var cacheKey = $"MatchGrade:v{hlaVersionProvider.GetActiveHlaDatabaseVersion()};l{locus};d{donorHlaName};p{patientHlaName}";
            return cache.GetOrAdd(cacheKey, func);
        }

        public MatchConfidence GetOrAddMatchConfidence(
            Locus? locus,
            string patientHlaName,
            string donorHlaName,
            Func<ICacheEntry, MatchConfidence> func)
        {
            var cacheKey = $"MatchConfidence:v{hlaVersionProvider.GetActiveHlaDatabaseVersion()};l{locus};d{donorHlaName};p{patientHlaName}";
            return cache.GetOrAdd(cacheKey, func);
        }
    }
}