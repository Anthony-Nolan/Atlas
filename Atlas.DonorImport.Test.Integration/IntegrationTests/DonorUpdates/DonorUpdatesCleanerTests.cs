using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorUpdates;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Atlas.DonorImport.ExternalInterface.Settings;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.DonorUpdates
{
    [TestFixture]
    internal class DonorUpdatesCleanerTest
    {
        private IDonorUpdatesCleaner updatesCleaner;
        private IDonorUpdatesPublisher updatesPublisher;
        private IPublishableDonorUpdatesRepository updatesRepository;
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
                updatesPublisher = DependencyInjection.DependencyInjection.Provider.GetService<IDonorUpdatesPublisher>();
                updatesRepository = DependencyInjection.DependencyInjection.Provider.GetService<IPublishableDonorUpdatesRepository>();
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
        public async Task DeleteExpiredPublishedDonorUpdates_NoExpirySet_DoesNotDeleteAnyDonorUpdates()
        {
            // note, if max batch size changes (either hard-coded value or configured), this test would fail
            const int publishBatchSize = 2000;
            const int nonPublishedCount = 96;
            const int totalCount = publishBatchSize + nonPublishedCount;

            await BuildAndImportDonors(totalCount);
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            var updatesCountBeforeDeletion = await updatesInspectionRepository.Count();

            SetUpCleaner(null);

            // ACT
            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            var updatesAfterDeletion = (await updatesInspectionRepository.GetAll()).ToList();

            updatesCountBeforeDeletion.Should().Be(totalCount);
            updatesAfterDeletion.Count.Should().Be(totalCount);
            updatesAfterDeletion.Count(u => u.IsPublished).Should().Be(publishBatchSize);
            updatesAfterDeletion.Count(u => !u.IsPublished).Should().Be(nonPublishedCount);
        }

        [Test]
        public async Task DeleteExpiredPublishedDonorUpdates_NoPublishedUpdates_DoesNotDeleteAnyDonorUpdates()
        {
            const int totalCount = 15;

            await BuildAndImportDonors(totalCount);
            var updatesCountBeforeDeletion = await updatesInspectionRepository.Count();

            // 0 days expiry should mean delete all published updates
            SetUpCleaner(0);

            // ACT
            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            var updatesAfterDeletion = (await updatesInspectionRepository.GetAll()).ToList();

            updatesCountBeforeDeletion.Should().Be(totalCount);
            updatesAfterDeletion.Count.Should().Be(totalCount);
            updatesAfterDeletion.Count(u => !u.IsPublished).Should().Be(totalCount);
        }

        [Test]
        public async Task DeleteExpiredPublishedDonorUpdates_OnlyDeletesExpiredPublishedDonorUpdates()
        {
            // note, if max batch size changes (either hard-coded value or configured), this test would fail
            const int publishBatchSize = 2000;
            const int nonPublishedCount = 82;
            const int totalCount = publishBatchSize + nonPublishedCount;

            await BuildAndImportDonors(totalCount);
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            var updatesCountBeforeDeletion = await updatesInspectionRepository.Count();

            // 0 days expiry should mean delete all published updates
            SetUpCleaner(0);

            // ACT
            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            var updatesAfterDeletion = (await updatesInspectionRepository.GetAll()).ToList();

            updatesCountBeforeDeletion.Should().Be(totalCount);
            updatesAfterDeletion.Count.Should().Be(nonPublishedCount);
            updatesAfterDeletion.Count(u => !u.IsPublished).Should().Be(nonPublishedCount);
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

        private void SetUpCleaner(int? expiryInDays)
        {
            var settings = new PublishDonorUpdatesSettings { PublishedUpdateExpiryInDays = expiryInDays };
            updatesCleaner = new DonorUpdatesCleaner(updatesRepository, settings);
        }
    }
}
