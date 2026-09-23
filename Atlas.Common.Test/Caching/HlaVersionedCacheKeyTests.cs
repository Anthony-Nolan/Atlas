using Atlas.Common.Caching;
using AwesomeAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Caching
{
    /// <summary>
    /// The key shapes here mirror the ones minted in production. Anything that changes a cache key format without
    /// changing these is the failure mode this fixture exists to catch: an unmatched key is never evicted, which is
    /// exactly the stale-cache bug of ATL-395.
    /// </summary>
    [TestFixture]
    public class HlaVersionedCacheKeyTests
    {
        private const string Version = "3650";

        [TestCase("hlaMatchingLookup:3650", TestName = "Whole-table cache - CloudTableRepositoryBase.VersionedCacheKey")]
        [TestCase("All-P-Groups:3650", TestName = "HlaMatchingMetadataRepository")]
        [TestCase("All-G-Groups:3650", TestName = "HlaScoringMetadataRepository")]
        [TestCase("All-small-g-Groups:3650", TestName = "SmallGGroupToPGroupMetadataRepository")]
        [TestCase("hlaMetadataDictionary-version:3650", TestName = "HlaMetadataDictionaryFactory")]
        [TestCase("AlleleNamesMetadataService-3650-A-01:01", TestName = "Per-lookup outcome - MetadataServiceBase")]
        [TestCase("hlaScoringLookup-3650-A-0265", TestName = "Per-lookup outcome - the ATL-395 serology code")]
        [TestCase("MatchGrade:v3650;lA;d01:01;p01:01", TestName = "ScoringCache - match grade")]
        [TestCase("MatchConfidence:v3650;lA;d01:01;p01:01", TestName = "ScoringCache - match confidence")]
        [TestCase("IsAntigenMatch:v3650;lA;d01:01;p01:01", TestName = "ScoringCache - antigen match")]
        public void Matches_ForAKeyOfThisVersion_IsTrue(string cacheKey)
        {
            HlaVersionedCacheKey.Matches(cacheKey, Version).Should().BeTrue();
        }

        [TestCase("hlaMatchingLookup:3660")]
        [TestCase("All-P-Groups:3660")]
        [TestCase("hlaScoringLookup-3660-A-0265")]
        [TestCase("MatchGrade:v3660;lA;d01:01;p01:01")]
        public void Matches_ForAKeyOfAnotherVersion_IsFalse(string cacheKey)
        {
            HlaVersionedCacheKey.Matches(cacheKey, Version).Should().BeFalse();
        }

        /// <summary>
        /// Versions are bare digit strings, so an unanchored substring match would let a shorter version evict a
        /// longer one's data - and, during a data refresh, that is the version every running app is still serving.
        /// </summary>
        [TestCase("hlaMatchingLookup:36500")]
        [TestCase("hlaScoringLookup-36500-A-0265")]
        [TestCase("MatchGrade:v36500;lA;d01:01;p01:01")]
        public void Matches_ForAVersionThisOneIsAPrefixOf_IsFalse(string cacheKey)
        {
            HlaVersionedCacheKey.Matches(cacheKey, Version).Should().BeFalse();
        }

        /// <summary>
        /// Data that isn't derived from the dictionary shares this cache - most expensively Match Prediction's
        /// haplotype frequency sets, which repopulate through a slow background warm. Evicting those on every
        /// dictionary recreation is the collateral damage this predicate exists to avoid.
        /// </summary>
        [TestCase("haplotype-frequency-set:123")]
        [TestCase("latestHlaNomenclatureVersionFromWmda")]
        [TestCase("mac:ABC")]
        public void Matches_ForDataNotDerivedFromTheDictionary_IsFalse(string cacheKey)
        {
            HlaVersionedCacheKey.Matches(cacheKey, Version).Should().BeFalse();
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Matches_ForAnAbsentKeyOrVersion_IsFalse(string absent)
        {
            HlaVersionedCacheKey.Matches(absent, Version).Should().BeFalse();
            HlaVersionedCacheKey.Matches("hlaMatchingLookup:3650", absent).Should().BeFalse();
        }
    }
}
