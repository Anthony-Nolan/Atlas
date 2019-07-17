using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Common.Repositories.DonorUpdates;
using Nova.SearchAlgorithm.Data.Persistent.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Models.AzureManagement;
using Nova.SearchAlgorithm.Services.AzureManagement;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;
using Nova.SearchAlgorithm.Services.DataRefresh;
using Nova.SearchAlgorithm.Settings;
using Nova.SearchAlgorithm.Test.Builders.DataRefresh;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public class DataRefreshServiceTests
    {
        private const string DefaultHlaDatabaseVersion = "3330";

        private IOptions<DataRefreshSettings> settingsOptions;
        private IActiveDatabaseProvider activeDatabaseProvider;
        private IAzureFunctionManager azureFunctionManager;
        private IAzureDatabaseManager azureDatabaseManager;
        private IDonorImportRepository donorImportRepository;
        private IRecreateHlaLookupResultsService recreateMatchingDictionaryService;
        private IDonorImporter donorImporter;
        private IHlaProcessor hlaProcessor;

        private IDataRefreshService dataRefreshService;

        [SetUp]
        public void SetUp()
        {
            settingsOptions = Substitute.For<IOptions<DataRefreshSettings>>();
            activeDatabaseProvider = Substitute.For<IActiveDatabaseProvider>();
            azureFunctionManager = Substitute.For<IAzureFunctionManager>();
            azureDatabaseManager = Substitute.For<IAzureDatabaseManager>();
            donorImportRepository = Substitute.For<IDonorImportRepository>();
            recreateMatchingDictionaryService = Substitute.For<IRecreateHlaLookupResultsService>();
            donorImporter = Substitute.For<IDonorImporter>();
            hlaProcessor = Substitute.For<IHlaProcessor>();

            settingsOptions.Value.Returns(DataRefreshSettingsBuilder.New.Build());

            dataRefreshService = new DataRefreshService(
                settingsOptions,
                activeDatabaseProvider,
                azureFunctionManager,
                azureDatabaseManager,
                donorImportRepository,
                recreateMatchingDictionaryService,
                donorImporter,
                hlaProcessor
            );
        }

        [Test]
        public async Task RefreshData_StopsDonorImportFunction()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await azureFunctionManager.Received().StopFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
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
        public async Task RefreshData_RestartsDonorImportFunction()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await azureFunctionManager.Received().StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
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
        public async Task RefreshData_ScalesActiveDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetActiveDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            await azureDatabaseManager.UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
        }

        [Test]
        public async Task RefreshData_RunsAzureSetUp_BeforeRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            Received.InOrder(() =>
            {
                azureFunctionManager.StopFunction(Arg.Any<string>(), Arg.Any<string>());
                donorImporter.ImportDonors();
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureTearDown_AfterRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .With(s => s.ActiveDatabaseSize, "S4")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            Received.InOrder(() =>
            {
                donorImporter.ImportDonors();
                azureFunctionManager.StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureFunctionsSetUp_BeforeTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .With(s => s.ActiveDatabaseSize, "S4")
                .With(s => s.DormantDatabaseSize, "S0")
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            Received.InOrder(() =>
            {
                azureFunctionManager.StopFunction(Arg.Any<string>(), Arg.Any<string>());
                azureFunctionManager.StartFunction(settings.DonorFunctionsAppName, settings.DonorImportFunctionName);
            });
        }
        [Test]
        public async Task RefreshData_RunsAzureDatabaseSetUp_BeforeTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DonorFunctionsAppName, "functions-app")
                .With(s => s.DonorImportFunctionName, "import-func")
                .With(s => s.ActiveDatabaseSize, "S4")
                .With(s => s.DormantDatabaseSize, "S0")
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshService.RefreshData(DefaultHlaDatabaseVersion);

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S0);
            });
        }
    }
}