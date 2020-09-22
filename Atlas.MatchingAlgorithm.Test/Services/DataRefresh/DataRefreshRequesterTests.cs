using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Utils.Http;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshRequesterTests
    {
        private IWmdaHlaNomenclatureVersionAccessor wmdaHlaNomenclatureVersionAccessor;
        private IActiveHlaNomenclatureVersionAccessor activeHlaVersionAccessor;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private IDataRefreshServiceBusClient serviceBusClient;
        private IDataRefreshSupportNotificationSender supportNotificationSender;

        private IDataRefreshRequester dataRefreshRequester;

        private const string ExistingHlaVersion = "old";
        private const string NewHlaVersion = "new";
        private const int NewRecordId = 123;

        [SetUp]
        public void SetUp()
        {
            wmdaHlaNomenclatureVersionAccessor = Substitute.For<IWmdaHlaNomenclatureVersionAccessor>();
            activeHlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            serviceBusClient = Substitute.For<IDataRefreshServiceBusClient>();
            supportNotificationSender = Substitute.For<IDataRefreshSupportNotificationSender>();

            dataRefreshRequester = BuildDataRefreshRequester();

            dataRefreshHistoryRepository.Create(default).ReturnsForAnyArgs(NewRecordId);
        }

        private DataRefreshRequester BuildDataRefreshRequester()
        {
            var hlaMetadataDictionaryBuilder = new HlaMetadataDictionaryBuilder().Using(wmdaHlaNomenclatureVersionAccessor);

            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(true);
            activeHlaVersionAccessor.GetActiveHlaNomenclatureVersion().Returns(ExistingHlaVersion);

            return new DataRefreshRequester(
                hlaMetadataDictionaryBuilder,
                activeHlaVersionAccessor,
                activeDatabaseProvider,
                dataRefreshHistoryRepository,
                serviceBusClient,
                supportNotificationSender
            );
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task RequestDataRefresh_WhenJobAlreadyInProgress_ThrowsHttpException(bool shouldForce)
        {
            dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Returns(new List<DataRefreshRecord> { new DataRefreshRecord() });

            await dataRefreshRequester.Invoking(service => service.RequestDataRefresh(
                    new DataRefreshRequest { ForceDataRefresh = shouldForce }))
                .Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task RequestDataRefresh_WhenActiveHlaVersionMatchesLatest_AndNonForcedMode_ThrowsHttpException()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            await dataRefreshRequester.Invoking(service => service.RequestDataRefresh(new DataRefreshRequest()))
                .Should().ThrowAsync<AtlasHttpException>();
        }

        [Test]
        public async Task RequestDataRefresh_WhenNoActiveHlaVersion_CreatesRecord()
        {
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(false);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database != null &&
                r.RefreshRequestedUtc != null &&
                string.IsNullOrEmpty(r.HlaNomenclatureVersion)));
        }

        [Test]
        public async Task RequestDataRefresh_WhenNoActiveHlaVersion_SendsMessage()
        {
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(false);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await serviceBusClient.Received().PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(x =>
                x.DataRefreshRecordId == NewRecordId));
        }

        [Test]
        public async Task RequestDataRefresh_WhenNoActiveHlaVersion_SendsNotification()
        {
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(false);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await supportNotificationSender.Received().SendInitialisationNotification(NewRecordId);
        }

        [Test]
        public async Task RequestDataRefresh_WhenNoActiveHlaVersion_ReturnsRecordId()
        {
            activeHlaVersionAccessor.DoesActiveHlaNomenclatureVersionExist().Returns(false);

            var result = await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            result.DataRefreshRecordId.Should().Be(NewRecordId);
        }

        [Test]
        public async Task RequestDataRefresh_WhenLatestHlaVersionHigherThanCurrent_CreatesRecord()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database != null &&
                r.RefreshRequestedUtc != null &&
                string.IsNullOrEmpty(r.HlaNomenclatureVersion)));
        }

        [Test]
        public async Task RequestDataRefresh_WhenLatestHlaVersionHigherThanCurrent_SendsMessage()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await serviceBusClient.Received().PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(x =>
                x.DataRefreshRecordId == NewRecordId));
        }

        [Test]
        public async Task RequestDataRefresh_WhenLatestHlaVersionHigherThanCurrent_SendsNotification()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await supportNotificationSender.Received().SendInitialisationNotification(NewRecordId);
        }

        [Test]
        public async Task RequestDataRefresh_WhenLatestHlaVersionHigherThanCurrent_ReturnsRecordId()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);

            var result = await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            result.DataRefreshRecordId.Should().Be(NewRecordId);
        }

        [Test]
        public async Task RequestDataRefresh_WhenActiveHlaVersionMatchesLatest_AndShouldForceRefresh_CreatesRecord()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest { ForceDataRefresh = true });

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database != null &&
                r.RefreshRequestedUtc != null &&
                string.IsNullOrEmpty(r.HlaNomenclatureVersion)));
        }

        [Test]
        public async Task RequestDataRefresh_WhenActiveHlaVersionMatchesLatest_AndShouldForceRefresh_SendsMessage()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest { ForceDataRefresh = true });
            
            await serviceBusClient.Received().PublishToRequestTopic(Arg.Is<ValidatedDataRefreshRequest>(x =>
                x.DataRefreshRecordId == NewRecordId));
        }

        [Test]
        public async Task RequestDataRefresh_WhenActiveHlaVersionMatchesLatest_AndShouldForceRefresh_SendsNotification()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest { ForceDataRefresh = true });

            await supportNotificationSender.Received().SendInitialisationNotification(NewRecordId);
        }

        [Test]
        public async Task RequestDataRefresh_WhenActiveHlaVersionMatchesLatest_AndShouldForceRefresh_ReturnsRecordId()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(ExistingHlaVersion);

            var result = await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest { ForceDataRefresh = true });

            result.DataRefreshRecordId.Should().Be(NewRecordId);
        }

        [Test]
        public async Task RequestDataRefresh_WhenDatabaseAActive_StoresRefreshRecordOfDatabaseB()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseB);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseB"
            ));
        }

        [Test]
        public async Task RequestDataRefresh_WhenDatabaseBActive_StoresRefreshRecordOfDatabaseA()
        {
            wmdaHlaNomenclatureVersionAccessor.GetLatestStableHlaNomenclatureVersion().ReturnsForAnyArgs(NewHlaVersion);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshRequester.RequestDataRefresh(new DataRefreshRequest());

            await dataRefreshHistoryRepository.Received().Create(Arg.Is<DataRefreshRecord>(r =>
                r.Database == "DatabaseA"
            ));
        }
    }
}