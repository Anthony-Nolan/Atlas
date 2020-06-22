using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
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
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {
        private IOptions<DataRefreshSettings> settingsOptions;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IHlaMetadataDictionary hlaMetadataDictionary;
        private IDonorImporter donorImporter;
        private IHlaProcessor hlaProcessor;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;
        private IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        private IDataRefreshRunner dataRefreshRunner;
        private ILogger logger;
        private IDormantRepositoryFactory transientRepositoryFactory;

        [SetUp]
        public void SetUp()
        {
            settingsOptions = Substitute.For<IOptions<DataRefreshSettings>>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            transientRepositoryFactory = Substitute.For<IDormantRepositoryFactory>();
            hlaMetadataDictionary = Substitute.For<IHlaMetadataDictionary>();
            donorImporter = Substitute.For<IDonorImporter>();
            hlaProcessor = Substitute.For<IHlaProcessor>();
            logger = Substitute.For<ILogger>();
            dataRefreshNotificationSender = Substitute.For<IDataRefreshNotificationSender>();
            dataRefreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();

            dataRefreshHistoryRepository.GetRecord(default).ReturnsForAnyArgs(DataRefreshRecordBuilder.New.Build());
            transientRepositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            settingsOptions.Value.Returns(DataRefreshSettingsBuilder.New.Build());

            dataRefreshRunner = new DataRefreshRunner(
                settingsOptions,
                activeDatabaseProvider,
                new AzureDatabaseNameProvider(settingsOptions),
                azureDatabaseManager,
                transientRepositoryFactory,
                new HlaMetadataDictionaryBuilder().Returning(hlaMetadataDictionary),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>(),
                donorImporter,
                hlaProcessor,
                logger,
                dataRefreshNotificationSender,
                dataRefreshHistoryRepository
            );
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
        public async Task RefreshData_ProcessesDonorHla_WithRefreshHlaNomenclatureVersion()
        {
            const string hlaNomenclatureVersion = "3390";
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(true);
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaNomenclatureVersion);

            await dataRefreshRunner.RefreshData(default);

            await hlaProcessor.Received().UpdateDonorHla(hlaNomenclatureVersion);
        }

        [Test]
        public async Task RefreshData_WhenTeardownFails_SendsAlert()
        {
            const AzureDatabaseSize databaseSize = AzureDatabaseSize.S0;
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, databaseSize.ToString())
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(Arg.Any<string>()).Throws(new Exception());
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
    }
}