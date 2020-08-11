using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Services.DataRefresh.DonorImport;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IHlaMetadataDictionary hlaMetadataDictionary;
        private IDonorImporter donorImporter;
        private IHlaProcessor hlaProcessor;
        private IDonorUpdateProcessor donorUpdateProcessor;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshRunner dataRefreshRunner;
        private IMatchingAlgorithmImportLogger logger;
        private IDormantRepositoryFactory transientRepositoryFactory;

        [SetUp]
        public void SetUp()
        {
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            transientRepositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            hlaMetadataDictionary = Substitute.For<IHlaMetadataDictionary>();
            donorImporter = Substitute.For<IDonorImporter>();
            hlaProcessor = Substitute.For<IHlaProcessor>();
            donorUpdateProcessor = Substitute.For<IDonorUpdateProcessor>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshNotificationSender>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.Build());
            transientRepositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);

            dataRefreshRunner = BuildDataRefreshRunner();
        }

        [Test]
        public async Task RefreshData_RemovesAllDonorInformation()
        {
            await dataRefreshRunner.RefreshData(default);

            await donorImportRepository.Received().RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_ReportsHlaMetadataWasRecreated_WithRefreshHlaNomenclatureVersion()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(true);
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaNomenclatureVersion);

            var returnedHlaVersion = await dataRefreshRunner.RefreshData(default);

            returnedHlaVersion.Should().Be(hlaNomenclatureVersion);
        }

        [Test]
        public async Task RefreshData_ImportsDonors()
        {
            await dataRefreshRunner.RefreshData(default);

            await donorImporter.Received().ImportDonors();
        }

        [Test]
        public async Task RefreshData_WhenRunningMetadataDictionaryStep_RecordsLatestVersion()
        {
            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(
                DataRefreshRecordBuilder.New.Build()
            );
            var version = "latestHlaVersion";
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest).Returns(version);

            await dataRefreshRunner.RefreshData(default);

            await dataRefreshHistoryRepository.Received().UpdateExecutionDetails(Arg.Any<int>(), version);
        }

        [Test]
        public async Task RefreshData_WhenRunningFromScratch_PassesRefreshHlaVersionToLaterSteps()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaNomenclatureVersion);

            await dataRefreshRunner.RefreshData(default);

            await hlaProcessor.Received().UpdateDonorHla(hlaNomenclatureVersion, Arg.Any<Func<int, Task>>());
        }

        [Test]
        public async Task RefreshData_WhenTeardownFails_SendsAlert()
        {
            const AzureDatabaseSize databaseSize = AzureDatabaseSize.S0;
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, databaseSize.ToString())
                .Build();

            dataRefreshRunner = BuildDataRefreshRunner(settings);

            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(default, default).ThrowsForAnyArgs(new Exception());
            azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), databaseSize).Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await dataRefreshNotificationSender.ReceivedWithAnyArgs().SendTeardownFailureAlert(default);
            }
        }

        private IDataRefreshRunner BuildDataRefreshRunner(DataRefreshSettings dataRefreshSettings = null)
        {
            var settings = dataRefreshSettings ?? DataRefreshSettingsBuilder.New.Build();
            return new DataRefreshRunner(
                settings,
                activeDatabaseProvider,
                new AzureDatabaseNameProvider(settings),
                azureDatabaseManager,
                transientRepositoryFactory,
                new HlaMetadataDictionaryBuilder().Returning(hlaMetadataDictionary),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>(),
                donorImporter,
                hlaProcessor,
                donorUpdateProcessor,
                logger,
                dataRefreshNotificationSender,
                dataRefreshHistoryRepository,
                new MatchingAlgorithmImportLoggingContext()
            );
        }
    }
}