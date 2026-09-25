using System;

namespace Atlas.Common.Caching
{
    /// <summary>
    /// Recognises persistent-cache keys that hold data derived from the HLA Metadata Dictionary at a given HLA
    /// nomenclature version, so that recreating the dictionary can evict exactly those entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This lives in Atlas.Common rather than in the HLA Metadata Dictionary because the keys it has to recognise are
    /// minted in two projects - the dictionary's own repositories and services, and the Matching Algorithm's
    /// <c>ScoringCache</c>, whose entries are keyed by nomenclature version and go stale at the same moment.
    /// </para>
    ///
    /// <para>
    /// The version is deliberately NOT matched as a bare substring. Versions are plain digit strings ("3650"), so an
    /// unanchored match would let "365" match a "3650" entry. Every shape below is anchored on the delimiters that
    /// surround the version in the key that produced it.
    /// </para>
    /// </remarks>
    public static class HlaVersionedCacheKey
    {
        /// <summary>
        /// Whether <paramref name="cacheKey"/> holds data derived from the dictionary at
        /// <paramref name="hlaNomenclatureVersion"/>.
        /// </summary>
        /// <remarks>
        /// Biased towards over-matching, deliberately. Evicting an entry belonging to another version costs one
        /// re-read from Azure Table Storage; FAILING to evict an entry belonging to this version is the bug this
        /// exists to fix, and is invisible until someone notices a donor being silently skipped. So where the two
        /// risks trade off - see the <c>:{version}</c> shape below - this errs towards evicting.
        /// </remarks>
        public static bool Matches(string cacheKey, string hlaNomenclatureVersion)
        {
            if (string.IsNullOrWhiteSpace(cacheKey) || string.IsNullOrWhiteSpace(hlaNomenclatureVersion))
            {
                return false;
            }

            var version = hlaNomenclatureVersion;

            // "{cacheKey}:{version}", e.g. "hlaMatchingLookup:3650", "All-P-Groups:3650",
            // "hlaMetadataDictionary-version:3650" - the whole-collection caches and the Factory's cached dictionary.
            //
            // A per-lookup key (the shape below) ends with the HLA name looked up, and an HLA name CAN end in
            // ":3650" - "A*01:3650" is syntactically well-formed - so this can match an entry belonging to another
            // version. That over-eviction is accepted; see the remarks above.
            if (cacheKey.EndsWith($":{version}", StringComparison.Ordinal))
            {
                return true;
            }

            // "{perTypeCacheKey}-{version}-{locus}-{lookupName}", e.g. "hlaScoringLookup-3650-A-0265" - the per-lookup
            // outcomes cached by MetadataServiceBase, INCLUDING its cached "this name has no data" results. Those are
            // what made the defect present as a silently skipped donor: the serology code was cached as not-found before
            // the dictionary was recreated with a row for it.
            if (cacheKey.Contains($"-{version}-", StringComparison.Ordinal))
            {
                return true;
            }

            // "{Prefix}:v{version};l{locus};d{donor};p{patient}" - the Matching Algorithm's ScoringCache. ';' cannot
            // appear in an HLA name, so this shape cannot be produced by accident.
            return cacheKey.Contains($":v{version};", StringComparison.Ordinal);
        }
    }
}
