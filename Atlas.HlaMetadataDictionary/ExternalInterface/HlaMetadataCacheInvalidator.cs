using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    /// <summary>
    /// Drops this process's in-memory copy of the HLA Metadata Dictionary at a given nomenclature version, so that the
    /// next lookup for that version re-reads Azure Table Storage.
    /// </summary>
    /// <remarks>
    /// Needed because every in-memory cache of dictionary data is keyed by nomenclature version and nothing else, so
    /// a recreation at an unchanged version leaves all of them holding data that no longer matches storage. See
    /// <c>TableClientRepositoryBase</c> (whole tables), <c>MetadataServiceBase</c> (individual lookups, including
    /// cached "this name has no data" outcomes), <c>HlaMetadataDictionaryFactory</c>, and the Matching Algorithm's
    /// <c>ScoringCache</c>.
    /// </remarks>
    public interface IHlaMetadataCacheInvalidator
    {
        /// <param name="hlaNomenclatureVersion">
        /// The version that was recreated. Only data cached for this version is dropped.
        /// </param>
        void InvalidateCaches(string hlaNomenclatureVersion);
    }

    /// <remarks>
    /// Scoped to the one version, which matters most during a data refresh. A refresh recreates the dictionary at a
    /// NEW version as its first stage, while every running matching app carries on serving the PREVIOUS version for
    /// the hours until that refresh completes - the active version being the one from the last successful refresh
    /// (<c>DataRefreshHistoryRepository.GetActiveHlaNomenclatureVersion</c>). Evicting by version leaves those apps'
    /// warm caches untouched; emptying the cache wholesale would cold-start them on every scheduled refresh.
    ///
    /// <para>
    /// Which keys belong to a version is decided by <see cref="HlaVersionedCacheKey"/>. Data that is not derived from
    /// the dictionary - most expensively Match Prediction's haplotype frequency sets, which repopulate through a slow
    /// background warm - shares this cache but carries no version in its keys, and so is never matched.
    /// </para>
    /// </remarks>
    internal class HlaMetadataCacheInvalidator : IHlaMetadataCacheInvalidator
    {
        private readonly IPersistentCacheProvider cacheProvider;
        private readonly IAtlasLogger logger;

        public HlaMetadataCacheInvalidator(IPersistentCacheProvider cacheProvider, IAtlasLogger logger)
        {
            this.cacheProvider = cacheProvider;
            this.logger = logger;
        }

        public void InvalidateCaches(string hlaNomenclatureVersion)
        {
            cacheProvider.RemoveWhere(key => HlaVersionedCacheKey.Matches(key, hlaNomenclatureVersion));

            logger.SendTrace(
                "HLA-METADATA-DICTIONARY REFRESH: Cleared in-memory caches following recreation of the dictionary at " +
                $"HLA Nomenclature version: {hlaNomenclatureVersion}.");
        }
    }
}
