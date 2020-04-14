using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.MatchingDictionary.Services;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Services.AzureManagement;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Builders.DataRefresh;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshServiceTests
    {
        private const string DefaultHlaDatabaseVersion = "3330";

        private IOptions<DataRefreshSettings> settingsOptions;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IRecreateHlaLookupResultsService recreateMatchingDictionaryService;
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
            recreateMatchingDictionaryService = Substitute.For<IRecreateHlaLookupResultsService>();
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
                recreateMatchingDictionaryService,
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

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await azureDatabaseManager.UpdateDatabaseSize(settings.DatabaseAName, Arg.Any<AzureDatabaseSize>());
        }

        [Test]
        public async Task RefreshData_ScalesDormantDatabaseToRefreshSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
        }

        [Test]
        public async Task RefreshData_RemovesAllDonorInformation()
        {
            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await donorImportRepository.Received().RemoveAllDonorInformation();
        }

        [Test]
        public async Task RefreshData_RecreatesMatchingDictionaryWithRefreshHlaDatabaseVersion()
        {
            const string hlaDatabaseVersion = "3390";

            await dataRefreshService.RefreshData(hlaDatabaseVersion);

            await recreateMatchingDictionaryService.Received().RecreateAllHlaLookupResults(hlaDatabaseVersion);
        }

        [Test]
        public async Task RefreshData_ImportsDonors()
        {
            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await donorImporter.Received().ImportDonors();
        }

        [Test]
        public async Task RefreshData_ProcessesDonorHla_WithRefreshHlaDatabaseVersion()
        {
            const string hlaDatabaseVersion = "3390";

            await dataRefreshService.RefreshData(hlaDatabaseVersion);

            await hlaProcessor.Received().UpdateDonorHla(hlaDatabaseVersion);
        }

        [Test]
        public async Task RefreshData_ScalesRefreshDatabaseToActiveSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "S4")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
        }

        [Test]
        public async Task RefreshData_RunsAzureSetUp_BeforeRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

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

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

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

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
            });
        }

        [Test]
        public async Task RefreshData_WhenMatchingDictionaryRecreationFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            recreateMatchingDictionaryService.RecreateAllHlaLookupResults(Arg.Any<string>()).Throws(new Exception());

            try
            {
                await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);
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
                await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);
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
                await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);
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
                await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);
            }
            catch (Exception)
            {
                await dataRefreshNotificationSender.Received().SendTeardownFailureAlert();
            }
        }

        [Test]
        public async Task RefreshData_WhenContinued_DoesNotRemoveOldData()
        {
            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion, true);

            await donorImportRepository.DidNotReceive().RemoveAllDonorInformation();
        }
    }
}