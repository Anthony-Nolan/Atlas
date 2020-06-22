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
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh
{
    [TestFixture]
    public partial class DataRefreshRunnerTests
    {

        [Test]
        public async Task RefreshData_ScalesDormantDatabase()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.DatabaseAName, "db-a")
                .With(s => s.DatabaseBName, "db-b")
                .Build();
            settingsOptions.Value.Returns(settings);
            activeDatabaseProvider.GetDormantDatabase().Returns(TransientDatabase.DatabaseA);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.UpdateDatabaseSize(settings.DatabaseAName, Arg.Any<AzureDatabaseSize>());
        }

        [Test]
        public async Task RefreshData_ScalesDormantDatabaseToRefreshSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.P15);
        }

        [Test]
        public async Task RefreshData_ScalesRefreshDatabaseToActiveSize()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "S4")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

            await azureDatabaseManager.UpdateDatabaseSize(Arg.Any<string>(), AzureDatabaseSize.S4);
        }

        [Test]
        public async Task RefreshData_RunsAzureSetUp_BeforeRefresh()
        {
            var settings = DataRefreshSettingsBuilder.New
                .With(s => s.RefreshDatabaseSize, "P15")
                .Build();
            settingsOptions.Value.Returns(settings);

            await dataRefreshRunner.RefreshData(default);

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

            await dataRefreshRunner.RefreshData(default);

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

            await dataRefreshRunner.RefreshData(default);

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
                await dataRefreshRunner.RefreshData(default);
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
                await dataRefreshRunner.RefreshData(default);
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
                await dataRefreshRunner.RefreshData(default);
            }
            catch (Exception)
            {
                await azureDatabaseManager.Received().UpdateDatabaseSize(settings.DatabaseAName, AzureDatabaseSize.S0);
            }
        }
    }
}