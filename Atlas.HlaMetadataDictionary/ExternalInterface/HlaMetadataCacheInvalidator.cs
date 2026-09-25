using Atlas.Common.ApplicationInsights;
using Atlas.Common.Caching;

namespace Atlas.HlaMetadataDictionary.ExternalInterface
{
    /// <summary>
    /// Drops this process's in-memory copy of the HLA Metadata Dictionary at a given nomenclature version, so that the
    /// next lookup for that version re-reads Azure Table Storage.
    /// </summary>
    public interface IHlaMetadataCacheInvalidator
    {
        /// <param name="hlaNomenclatureVersion">
        /// The version that was recreated. Only data cached for this version is dropped.
        /// </param>
        void InvalidateCaches(string hlaNomenclatureVersion);
    }

    /// <remarks>
    /// Scoped to the one version rather than emptying the cache: a data refresh recreates the dictionary at a NEW
    /// version hours before running apps stop serving the PREVIOUS one, so evicting wholesale would cold-start their
    /// warm caches on every scheduled refresh.
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
