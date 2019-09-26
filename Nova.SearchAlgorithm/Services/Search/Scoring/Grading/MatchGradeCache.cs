using System;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Services.Search.Scoring.Grading
{
    public interface IMatchGradeCache
    {
        MatchGrade GetOrAddMatchGrade(Locus locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchGrade> func);
    }

    public class MatchGradeCache : IMatchGradeCache
    {
        private readonly IAppCache cache;

        public MatchGradeCache(IAppCache cache)
        {
            this.cache = cache;
        }

        public MatchGrade GetOrAddMatchGrade(Locus locus, string patientHlaName, string donorHlaName, Func<ICacheEntry, MatchGrade> func)
        {
            var cacheKey = $"{locus}:d{donorHlaName}:p{patientHlaName}";
            return cache.GetOrAdd(cacheKey, func);
        }
    }
}