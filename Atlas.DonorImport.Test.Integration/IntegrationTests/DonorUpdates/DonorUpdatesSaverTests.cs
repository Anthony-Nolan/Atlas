using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.DonorUpdates
{
    [TestFixture]
    internal class DonorUpdatesSaverTest
    {
        private IPublishableDonorUpdatesInspectionRepository updatesInspectionRepository;
        private IDonorInspectionRepository donorInspectionRepository;
        private IDonorFileImporter fileImporter;

        private static Builder<DonorUpdate> DonorBuilder => DonorUpdateBuilder.New
            .With(upd => upd.ChangeType, ImportDonorChangeType.Upsert);
        private static readonly Builder<DonorImportFile> FileBuilder = DonorImportFileBuilder.NewWithoutContents;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                fileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                updatesInspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IPublishableDonorUpdatesInspectionRepository>();
                donorInspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                // Ensure any mocks set up for this test do not stick around.
                DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
                DatabaseManager.ClearDatabases();
            });
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearPublishableDonorUpdates);
        }

        [Test]
        public async Task ImportDonorFile_SavesUpdatesForEachDonor()
        {
            const int donorCount = 50;

            var donorIds = await BuildAndImportDonors(donorCount);

            var updates = (await updatesInspectionRepository.GetAll()).ToList();

            updates.Count.Should().Be(donorCount);
            updates.Select(u => u.DonorId).Should().BeEquivalentTo(donorIds);
            updates.Select(u => u.ToSearchableDonorUpdate().SearchableDonorInformation).Should().NotContainNulls();
        }

        [Test]
        public async Task ImportDonorFile_DefaultValueColumnsOnUpdatesAreSetCorrectly()
        {
            await BuildAndImportDonors(1);
            var dateTimeJustAfterUpdateCreation = DateTimeOffset.Now;

            var updates = (await updatesInspectionRepository.GetAll()).ToList();

            updates.Select(u => u.CreatedOn).Distinct().Single().Should().BeCloseTo(dateTimeJustAfterUpdateCreation, 10000);
            updates.Select(u => u.IsPublished).Should().AllBeEquivalentTo(false);
            updates.Select(u => u.PublishedOn).Should().AllBeEquivalentTo((DateTimeOffset?)null);
        }

        /// <summary>
        /// Importing donors creates new updates for publishing - this functionality is covered elsewhere.
        /// </summary>
        /// <returns>Atlas donor Ids</returns>
        private async Task<IEnumerable<int>> BuildAndImportDonors(int donorCount)
        {
            var donors = DonorBuilder.Build(donorCount).ToArray();
            await fileImporter.ImportDonorFile(FileBuilder.WithDonors(donors).Build());
            var externalCodes = donors.Select(d => d.RecordId).ToList();
            return (await donorInspectionRepository.GetDonorIdsByExternalDonorCodes(externalCodes)).Values;
        }
    }
}
