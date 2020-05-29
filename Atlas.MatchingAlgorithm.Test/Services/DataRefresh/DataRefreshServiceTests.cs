using System;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshServiceTests
    {
        private IOptions<DataRefreshSettings> settingsOptions;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IHlaMetadataDictionary hlaMetadataDictionary;
        private IDonorImporter donorImporter;
        private IHlaProcessor hlaProcessor;
        private IDataRefreshNotificationSender dataRefreshNotificationSender;

        private IDataRefreshService dataRefreshService;
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

            transientRepositoryFactory.GetDonorImportRepository().Returns(donorImportRepository);
            settingsOptions.Value.Returns(DataRefreshSettingsBuilder.New.Build());

            dataRefreshService = new DataRefreshService(
                settingsOptions,
                activeDatabaseProvider,
                new AzureDatabaseNameProvider(settingsOptions),
                azureDatabaseManager,
                transientRepositoryFactory,
                new HlaMetadataDictionaryBuilder().Returning(hlaMetadataDictionary),
                Substitute.For<IActiveHlaVersionAccessor>(),
                donorImporter,
                hlaProcessor,
                logger,
                dataRefreshNotificationSender
            );
        }
        
        [Test]
        public async Task RefreshData_ScalesDormantDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DatabaseBName, "db-b")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshService.RefreshData();

            await azureDatabaseManager.UpdateDatabaseSize(settings.DatabaseAName, Arg.Any<AzureDatabaseSize>());
        }

        [Test]
        public async Task RefreshData_ScalesDormantDatabaseToRefreshSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData();

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
        }

        [Test]
        public async Task RefreshData_RemovesAllDonorInformation()
        {
            await dataRefreshService.RefreshData();

            await donorImportRepository.Received().RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_ReportsHlaMetadataWasRecreated_WithRefreshHlaDatabaseVersion()
        {
            const string hlaDatabaseVersion = "3390";
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(true);
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaDatabaseVersion);

            var returnedHlaVersion = await dataRefreshService.RefreshData();

            returnedHlaVersion.Should().Be(hlaDatabaseVersion);
        }

        [Test]
        public async Task RefreshData_ImportsDonors()
        {
            await dataRefreshService.RefreshData();

            await donorImporter.Received().ImportDonors();
        }

        [Test]
        public async Task RefreshData_ProcessesDonorHla_WithRefreshHlaDatabaseVersion()
        {
            const string hlaDatabaseVersion = "3390";
            hlaMetadataDictionary.IsActiveVersionDifferentFromLatestVersion().Returns(true);
            hlaMetadataDictionary
                .RecreateHlaMetadataDictionary(CreationBehaviour.Latest)
                .Returns(hlaDatabaseVersion);

            await dataRefreshService.RefreshData();

            await hlaProcessor.Received().UpdateDonorHla(hlaDatabaseVersion);
        }

        [Test]
        public async Task RefreshData_ScalesRefreshDatabaseToActiveSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "S4")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData();

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
        }

        [Test]
        public async Task RefreshData_RunsAzureSetUp_BeforeRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData();

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
                donorImporter.ImportDonors();
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureTearDown_AfterRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, "S4")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData();

            Received.InOrder(() =>
            {
                donorImporter.ImportDonors();
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureDatabaseSetUp_BeforeTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, "S4")
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData();

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
            });
        }

        [Test]
        public async Task RefreshData_WhenHlaMetadataDictionaryRecreationFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest).Throws(new Exception());

            try
            {
                await dataRefreshService.RefreshData();
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
        }

        [Test]
        public async Task RefreshData_WhenDonorImportFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            donorImporter.ImportDonors().Throws(new Exception());

            try
            {
                await dataRefreshService.RefreshData();
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
        }

        [Test]
        public async Task RefreshData_WhenHlaProcessingFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(Arg.Any<string>()).Throws(new Exception());

            try
            {
                await dataRefreshService.RefreshData();
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
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
                await dataRefreshService.RefreshData();
            }
            catch (Exception)
            {
                await dataRefreshNotificationSender.Received().SendTeardownFailureAlert();
            }
        }
    }
}