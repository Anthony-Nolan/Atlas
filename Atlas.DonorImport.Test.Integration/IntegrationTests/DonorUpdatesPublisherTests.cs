using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests
{
    [TestFixture]
    internal class DonorUpdatesPublisherTests
    {
        private IDonorUpdatesPublisher updatesPublisher;
        private IMessageBatchPublisher<SearchableDonorUpdate> messagePublisher;
        private IPublishableDonorUpdatesInspectionRepository updatesInspectionRepository;
        private IDonorFileImporter fileImporter;

        private static Builder<DonorUpdate> DonorBuilder => DonorUpdateBuilder.New
            .With(upd => upd.ChangeType, ImportDonorChangeType.Upsert);
        private static readonly Builder<DonorImportFile> FileBuilder = DonorImportFileBuilder.NewWithoutContents;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                messagePublisher = Substitute.For<IMessageBatchPublisher<SearchableDonorUpdate>>();
                var services = DependencyInjection.ServiceConfiguration.BuildServiceCollection();
                services.AddScoped(sp => messagePublisher);
                DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();

                updatesPublisher = DependencyInjection.DependencyInjection.Provider.GetService<IDonorUpdatesPublisher>();
                fileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                updatesInspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IPublishableDonorUpdatesInspectionRepository>();
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
            messagePublisher.ClearReceivedCalls();
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearPublishableDonorUpdates);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_WhenNoUpdates_DoesNotPublishUpdateMessages()
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await messagePublisher.ReceivedWithAnyArgs(0).BatchPublish(default);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_PublishesOldestUpdateMessages()
        {
            // note, if max batch size changes (either hard-coded value or configured), this test would fail
            const int publishBatchSize = 2000;
            const int donorCount = publishBatchSize + 56;

            await BuildAndImportDonors(donorCount);
            var updatesBeforePublishing = await updatesInspectionRepository.GetAll();
            var donorIdsOfOldestUpdates = updatesBeforePublishing
                .OrderBy(u => u.Id)
                .Take(publishBatchSize)
                .Select(u => u.ToSearchableDonorUpdate().DonorId);

            // ACT
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await messagePublisher.Received().BatchPublish(Arg.Is<IEnumerable<SearchableDonorUpdate>>(x => 
                x.Count() == publishBatchSize &&
                x.Select(m => m.DonorId).SequenceEqual(donorIdsOfOldestUpdates)));
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_DeletesPublishedUpdates()
        {
            // note, if max batch size changes (either hard-coded value or configured), this test would fail
            const int publishBatchSize = 2000;
            const int expectedUnpublishedCount = 21;
            const int donorCount = publishBatchSize + expectedUnpublishedCount;

            await BuildAndImportDonors(donorCount);
            var updatesCountBeforePublish = await updatesInspectionRepository.Count();

            // ACT
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            var updatesCountAfterPublish = await updatesInspectionRepository.Count();

            updatesCountBeforePublish.Should().Be(donorCount);
            updatesCountAfterPublish.Should().Be(expectedUnpublishedCount);
        }

        // Importing donors creates new updates for publishing - this functionality is tested elsewhere.
        private async Task<IReadOnlyCollection<DonorUpdate>> BuildAndImportDonors(int donorCount)
        {
            var donors = DonorBuilder.Build(donorCount).ToArray();
            var importFile = FileBuilder.WithDonors(donors).Build();
            await fileImporter.ImportDonorFile(importFile);

            return donors.ToList();
        }
    }
}
