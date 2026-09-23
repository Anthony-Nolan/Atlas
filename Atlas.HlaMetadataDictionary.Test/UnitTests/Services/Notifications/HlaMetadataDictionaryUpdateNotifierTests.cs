using System.Text;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.Services.Notifications;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.Notifications
{
    [TestFixture]
    internal class HlaMetadataDictionaryUpdateNotifierTests
    {
        private const string DefaultVersion = "hla-version";
        private const string UpdatedTopic = "hla-metadata-dictionary-updated";

        private ITopicClientFactory topicClientFactory;
        private ITopicClient topicClient;
        private IHlaMetadataDictionaryUpdateNotifier notifier;

        [SetUp]
        public void SetUp()
        {
            topicClient = Substitute.For<ITopicClient>();
            topicClientFactory = Substitute.For<ITopicClientFactory>();
            topicClientFactory.BuildTopicClient(UpdatedTopic).Returns(topicClient);

            notifier = new ServiceBusHlaMetadataDictionaryUpdateNotifier(
                topicClientFactory,
                new HlaMetadataDictionaryNotificationSettings
                {
                    UpdatedTopic = UpdatedTopic,
                    SendRetryCount = 1,
                    SendRetryCooldownSeconds = 0
                },
                Substitute.For<IAtlasLogger>());
        }

        [Test]
        public async Task NotifyOfUpdate_PublishesTheRecreatedNomenclatureVersion()
        {
            await notifier.NotifyOfUpdate(DefaultVersion);

            await topicClient.Received(1).SendAsync(Arg.Is<ServiceBusMessage>(m =>
                JsonConvert.DeserializeObject<HlaMetadataDictionaryUpdatedMessage>(
                    Encoding.UTF8.GetString(m.Body.ToArray())).HlaNomenclatureVersion == DefaultVersion));
        }

        /// <summary>
        /// The regression guard for the defect this replaced: the notifier used to build its topic client once, in its
        /// constructor. The object that calls it - the dictionary the Factory hands out - is cached in the singleton
        /// persistent cache and outlives the DI scope that built it, so that client had already been disposed with its
        /// scope by the time a later recreation tried to publish, and the notification was never sent.
        /// </summary>
        [Test]
        public async Task NotifyOfUpdate_BuildsAFreshTopicClientOnEveryCall()
        {
            await notifier.NotifyOfUpdate(DefaultVersion);
            await notifier.NotifyOfUpdate(DefaultVersion);

            topicClientFactory.Received(2).BuildTopicClient(UpdatedTopic);
            await topicClient.Received(2).SendAsync(Arg.Any<ServiceBusMessage>());
        }

        [Test]
        public async Task NotifyOfUpdate_DisposesTheTopicClientItBuilt()
        {
            await notifier.NotifyOfUpdate(DefaultVersion);

            await topicClient.Received(1).DisposeAsync();
        }
    }
}
