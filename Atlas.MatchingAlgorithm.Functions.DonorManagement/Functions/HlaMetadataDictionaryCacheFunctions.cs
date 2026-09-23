using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    /// <summary>
    /// Drops this app's in-memory copy of the HLA Metadata Dictionary whenever the dictionary's stored data is
    /// recreated, so that the next lookup reads the new data rather than the snapshot taken before it.
    /// </summary>
    /// <remarks>
    /// Every in-memory cache of dictionary data is keyed by nomenclature version and nothing else, so a recreation at
    /// an unchanged version - which is exactly what <c>RefreshHlaMetadataDictionaryToSpecificVersion</c> does - is
    /// invisible to an already-running app. It would otherwise serve the stale snapshot for the full 24h cache
    /// lifetime, or until the worker process happened to restart; a donor carrying newly-added lookup data is
    /// silently skipped during matching in the meantime. See ATL-395.
    ///
    /// <para>
    /// The subscription this binds to belongs to THIS worker instance, and is created at startup rather than declared
    /// in Terraform - see <c>HlaMetadataDictionaryCacheInvalidationConfiguration</c> for why one subscription shared
    /// across an app's instances would leave all but one of them stale.
    /// </para>
    /// </remarks>
    public class HlaMetadataDictionaryCacheFunctions
    {
        private readonly IHlaMetadataCacheInvalidator cacheInvalidator;

        public HlaMetadataDictionaryCacheFunctions(IHlaMetadataCacheInvalidator cacheInvalidator)
        {
            this.cacheInvalidator = cacheInvalidator;
        }

        [Function(nameof(InvalidateHlaMetadataDictionaryCache))]
        public void InvalidateHlaMetadataDictionaryCache(
            [ServiceBusTrigger(
                "%HlaMetadataDictionaryNotifications:UpdatedTopic%",
                "%HlaMetadataDictionaryNotifications:UpdatedSubscription%",
                Connection = "HlaMetadataDictionaryNotifications:ConnectionString")]
            HlaMetadataDictionaryUpdatedMessage notification)
        {
            cacheInvalidator.InvalidateCaches(notification.HlaNomenclatureVersion);
        }
    }
}
