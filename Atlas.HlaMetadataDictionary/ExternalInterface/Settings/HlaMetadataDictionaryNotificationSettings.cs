using System.ComponentModel.DataAnnotations;

namespace Atlas.HlaMetadataDictionary.ExternalInterface.Settings
{
    /// <summary>
    /// Service Bus configuration for announcing, and hearing about, recreations of the dictionary's stored data.
    /// </summary>
    /// <remarks>
    /// Needed by every app that reads the dictionary, because every one of them caches its data in memory and has to
    /// be told when to drop it. Only the apps that can also recreate the dictionary bind this class - via
    /// <c>RegisterHlaMetadataDictionaryUpdateNotifications</c> - to publish; the rest use the same configuration
    /// section through their Service Bus trigger binding alone.
    ///
    /// <para>
    /// Separate from <see cref="HlaMetadataDictionarySettings"/> because it is a messaging concern rather than a
    /// storage one, and because the connection string it needs grants rights the dictionary's own settings do not.
    /// </para>
    /// </remarks>
    public class HlaMetadataDictionaryNotificationSettings
    {
        /// <summary>
        /// Must carry Manage rights on <see cref="UpdatedTopic"/>: each app creates a subscription of its own on
        /// startup. Scoped to that topic rather than the whole namespace.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string ConnectionString { get; set; }

        /// <summary>Topic that <see cref="Models.HlaMetadataDictionaryUpdatedMessage"/>s are published to.</summary>
        [Required(AllowEmptyStrings = false)]
        public string UpdatedTopic { get; set; }

        /// <summary>
        /// Subscription this worker instance consumes update notifications on. NOT configured by hand - it is derived
        /// per instance and injected into configuration at startup by
        /// <c>AddPerInstanceHlaMetadataDictionaryCacheInvalidationSubscription</c>, then read from there by the
        /// Service Bus trigger binding. Listed here so the section's shape is discoverable in one place.
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public string UpdatedSubscription { get; set; }

        /// <summary>
        /// How long an instance's subscription may sit idle before Service Bus deletes it, which is how subscriptions
        /// belonging to scaled-away instances are cleaned up. Must exceed Service Bus's 5 minute minimum. A running
        /// instance holds an open receiver, which counts as activity and keeps its own subscription alive.
        /// </summary>
        [Range(5, int.MaxValue)]
        public int SubscriptionAutoDeleteOnIdleMinutes { get; set; } = 60;

        [Range(1, int.MaxValue)]
        public int SendRetryCount { get; set; } = 5;

        [Range(0, int.MaxValue)]
        public int SendRetryCooldownSeconds { get; set; } = 20;
    }
}
