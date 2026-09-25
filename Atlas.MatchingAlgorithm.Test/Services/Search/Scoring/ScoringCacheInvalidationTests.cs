using Microsoft.Extensions.Caching.Memory;
using Atlas.Common.Caching;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.Client.Models.Common.Results;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring
{
    /// <summary>
    /// <see cref="ScoringCache"/> keys its entries by HLA nomenclature version, so they go stale at the moment the
    /// dictionary's stored data is recreated - but the key format lives here, in the Matching Algorithm, while the
    /// code that recognises it lives in <see cref="HlaVersionedCacheKey"/> in Atlas.Common.
    ///
    /// <para>
    /// This fixture is the guard on that seam. If a scoring cache key is ever reformatted without updating
    /// <see cref="HlaVersionedCacheKey"/>, the entries simply stop being evicted - a silent return of the stale-cache bug - and
    /// nothing else in the suite would notice.
    /// </para>
    /// </summary>
    [TestFixture]
    public class ScoringCacheInvalidationTests
    {
        private const string RecreatedVersion = "3650";
        private const string OtherVersion = "3660";

        private IPersistentCacheProvider cacheProvider;
        private IActiveHlaNomenclatureVersionAccessor versionAccessor;
        private IScoringCache scoringCache;

        [SetUp]
        public void SetUp()
        {
            cacheProvider = AppCacheBuilder.NewPersistentCacheProvider();
            versionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            scoringCache = new ScoringCache(cacheProvider, versionAccessor);
        }

        private void InvalidateCachesFor(string version) =>
            cacheProvider.RemoveWhere(key => HlaVersionedCacheKey.Matches(key, version));

        [Test]
        public void Invalidation_EvictsScoringEntriesForTheRecreatedVersion()
        {
            versionAccessor.GetActiveHlaNomenclatureVersion().Returns(RecreatedVersion);
            var callCount = 0;
            MatchGrade Score(ICacheEntry _) { callCount++; return MatchGrade.GGroup; }

            scoringCache.GetOrAddMatchGrade(Locus.A, "01:01", "01:01", Score);
            scoringCache.GetOrAddMatchGrade(Locus.A, "01:01", "01:01", Score);
            callCount.Should().Be(1, "the second lookup should have been served from cache");

            InvalidateCachesFor(RecreatedVersion);
            scoringCache.GetOrAddMatchGrade(Locus.A, "01:01", "01:01", Score);

            callCount.Should().Be(2, "invalidation should have forced the grade to be recalculated");
        }

        [Test]
        public void Invalidation_LeavesScoringEntriesForOtherVersionsIntact()
        {
            versionAccessor.GetActiveHlaNomenclatureVersion().Returns(OtherVersion);
            var callCount = 0;
            MatchConfidence Score(ICacheEntry _) { callCount++; return MatchConfidence.Definite; }

            scoringCache.GetOrAddMatchConfidence(Locus.A, "01:01", "01:01", Score);
            callCount.Should().Be(1);

            InvalidateCachesFor(RecreatedVersion);
            scoringCache.GetOrAddMatchConfidence(Locus.A, "01:01", "01:01", Score);

            callCount.Should().Be(1, "a version that was not recreated must keep its warm cache");
        }

        [Test]
        public void Invalidation_EvictsAntigenMatchEntriesForTheRecreatedVersion()
        {
            versionAccessor.GetActiveHlaNomenclatureVersion().Returns(RecreatedVersion);
            var callCount = 0;
            bool? Score(ICacheEntry _) { callCount++; return true; }

            scoringCache.GetOrAddIsAntigenMatch(Locus.A, "01:01", "01:01", Score);
            scoringCache.GetOrAddIsAntigenMatch(Locus.A, "01:01", "01:01", Score);
            callCount.Should().Be(1);

            InvalidateCachesFor(RecreatedVersion);
            scoringCache.GetOrAddIsAntigenMatch(Locus.A, "01:01", "01:01", Score);

            callCount.Should().Be(2);
        }
    }
}
