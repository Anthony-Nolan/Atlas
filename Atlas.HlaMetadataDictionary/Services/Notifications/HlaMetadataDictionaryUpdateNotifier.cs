using System;
using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;

namespace Atlas.HlaMetadataDictionary.Services.Notifications
{
    internal interface IHlaMetadataDictionaryUpdateNotifier
    {
        /// <summary>
        /// Announces that the dictionary's stored data has been recreated at <paramref name="hlaNomenclatureVersion"/>.
        /// </summary>
        Task NotifyOfUpdate(string hlaNomenclatureVersion);
    }

    internal sealed class ServiceBusHlaMetadataDictionaryUpdateNotifier : IHlaMetadataDictionaryUpdateNotifier
    {
        private readonly ITopicClientFactory topicClientFactory;
        private readonly string updatedTopic;
        private readonly int sendRetryCount;
        private readonly int sendRetryCooldownSeconds;
        private readonly IAtlasLogger logger;

        public ServiceBusHlaMetadataDictionaryUpdateNotifier(
            ITopicClientFactory topicClientFactory,
            HlaMetadataDictionaryNotificationSettings settings,
            IAtlasLogger logger)
        {
            this.topicClientFactory = topicClientFactory;
            updatedTopic = settings.UpdatedTopic;
            sendRetryCount = settings.SendRetryCount;
            sendRetryCooldownSeconds = settings.SendRetryCooldownSeconds;
            this.logger = logger;
        }

        public async Task NotifyOfUpdate(string hlaNomenclatureVersion)
        {
            var notification = new HlaMetadataDictionaryUpdatedMessage
            {
                HlaNomenclatureVersion = hlaNomenclatureVersion,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notification)));

            // Built per send, and deliberately not held in a field. The object that calls this - the dictionary the
            // Factory hands out - is cached in the singleton persistent cache and therefore outlives the DI scope that
            // built it, so a client captured in the constructor would already have been disposed with that scope by
            // the time a later recreation tried to use it. Recreations are rare and ServiceBusSender is cheap to
            // create (its AMQP link is opened lazily), so there is nothing to gain by holding one open.
            await using var topicClient = topicClientFactory.BuildTopicClient(updatedTopic);

            await topicClient.SendWithRetryAndWaitAsync(
                message,
                sendRetryCount,
                sendRetryCooldownSeconds,
                (exception, retryNumber) => logger.SendTrace(
                    $"Could not send HLA Metadata Dictionary update notification to Service Bus; attempt {retryNumber}/{sendRetryCount}; exception: {exception}",
                    LogLevel.Warn));

            logger.SendTrace(
                $"HLA-METADATA-DICTIONARY REFRESH: Published update notification for HLA Nomenclature version: {hlaNomenclatureVersion}.");
        }
    }

    /// <summary>
    /// Registered by default, and replaced only in apps that opt in via
    /// <c>RegisterHlaMetadataDictionaryUpdateNotifications</c>. An app that cannot recreate the dictionary has nothing
    /// to announce, and requiring every consumer to carry publish credentials for a topic it will never publish to
    /// would be worse than this no-op.
    /// </summary>
    internal sealed class NonPublishingHlaMetadataDictionaryUpdateNotifier : IHlaMetadataDictionaryUpdateNotifier
    {
        private readonly IAtlasLogger logger;

        public NonPublishingHlaMetadataDictionaryUpdateNotifier(IAtlasLogger logger)
        {
            this.logger = logger;
        }

        public Task NotifyOfUpdate(string hlaNomenclatureVersion)
        {
            // Worth a trace rather than silence: an app that recreates the dictionary but was never given notification
            // settings would otherwise leave every other app's cache stale with no evidence of why.
            logger.SendTrace(
                "HLA-METADATA-DICTIONARY REFRESH: Dictionary was recreated at HLA Nomenclature version: " +
                $"{hlaNomenclatureVersion}, but this app has no update-notification settings configured, so no " +
                "notification was published. Consumers will serve their cached copy until it expires.",
                LogLevel.Warn);

            return Task.CompletedTask;
        }
    }
}
