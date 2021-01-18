using System;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Models.AzureManagement;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Runner
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {
        //These tests are extensions of the Test setup defined in DataRefreshRunnerTests_Core
        // Separated for convenience, since there are a LOT of tests :)

        [Test]
        public async Task RefreshData_ScalesDormantDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DatabaseBName, "db-b")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, Arg.Any<AzureDatabaseSize>(), Arg.Any<int?>());
        }

        [Test]
        public async Task RefreshData_ScalesDormantDatabaseToRefreshSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received().UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15, Arg.Any<int?>());
        }

        [Test]
        public async Task RefreshData_ScalesRefreshDatabaseToActiveSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "S4")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.Received().UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4, Arg.Any<int?>());
        }

        [Test]
        public async Task RefreshData_RunsAzureSetUp_BeforeRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15, Arg.Any<int?>());
                donorImporter.ImportDonors();
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureTearDown_AfterRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, "S4")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                donorImporter.ImportDonors();
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4, Arg.Any<int?>());
            });
        }

        [Test]
        public async Task RefreshData_RunsAzureDatabaseSetUp_BeforeTearDown()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.ActiveDatabaseSize, "S4")
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);

            await dataRefreshRunner.RefreshData(default);

            Received.InOrder(() =>
            {
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15, Arg.Any<int?>());
                azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4, Arg.Any<int?>());
            });
        }

        [Test]
        public async Task RefreshData_WhenHlaMetadataDictionaryRecreationFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaMetadataDictionary.RecreateHlaMetadataDictionary(CreationBehaviour.Latest).Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0, Arg.Any<int?>());
            }
        }

        [Test]
        public async Task RefreshData_WhenDonorImportFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            donorImporter.ImportDonors().Throws(new Exception());

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0, Arg.Any<int?>());
            }
        }

        [Test]
        public async Task RefreshData_WhenHlaProcessingFails_ScalesRefreshDatabaseToDormantSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DormantDatabaseSize, "S0")
                .Build();
            dataRefreshRunner = BuildDataRefreshRunner(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);
            hlaProcessor.UpdateDonorHla(default, default).ThrowsForAnyArgs<Exception>();

            try
            {
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0, Arg.Any<int?>());
            }
        }
    }
}