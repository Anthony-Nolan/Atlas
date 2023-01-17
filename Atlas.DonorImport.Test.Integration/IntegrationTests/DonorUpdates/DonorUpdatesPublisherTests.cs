using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.FileSchema.Models;
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
using Atlas.DonorImport.Services.DonorUpdates;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.DonorUpdates
{
    [TestFixture]
    internal class DonorUpdatesPublisherTest
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
        public async Task PublishSearchableDonorUpdatesBatch_PublishesUpdateMessagesNotAlreadyPublishedByOldestFirst()
        {
            // note, if max batch size changes (either hard-coded value or configured), this test would fail
            const int publishBatchSize = 2000;
            const int donorCount = publishBatchSize*2;

            await BuildAndImportDonors(donorCount);
            var donorIdsOldestFirst = (await updatesInspectionRepository.GetAll())
                .OrderBy(u => u.Id)
                .Select(u => u.DonorId)
                .ToList();

            var expectedBatch1 = donorIdsOldestFirst.Take(publishBatchSize);
            var expectedBatch2 = donorIdsOldestFirst.Skip(publishBatchSize).Take(publishBatchSize);

            // ACT
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            Received.InOrder(() =>
            {
                messagePublisher.Received(1).BatchPublish(Arg.Is<IEnumerable<SearchableDonorUpdate>>(x =>
                    x.Count() == publishBatchSize && x.Select(m => m.DonorId).SequenceEqual(expectedBatch1)));
                messagePublisher.Received(1).BatchPublish(Arg.Is<IEnumerable<SearchableDonorUpdate>>(x =>
                    x.Count() == publishBatchSize && x.Select(m => m.DonorId).SequenceEqual(expectedBatch2)));
            });
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_MarksUpdatesAsPublished()
        {
            // note, if max batch size changes (either hard-coded value or configured), this test would fail
            const int publishBatchSize = 2000;
            const int notPublishedCount = 21;
            const int totalCount = publishBatchSize + notPublishedCount;

            await BuildAndImportDonors(totalCount);

            // ACT
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            var updates = (await updatesInspectionRepository.GetAll()).ToList();
            var published = updates.Where(u => u.IsPublished).ToList();
            var notPublished = updates.Where(u => !u.IsPublished).ToList();

            published.Count.Should().Be(publishBatchSize);
            notPublished.Count.Should().Be(notPublishedCount);
            published.Select(u => u.PublishedOn).Should().NotContainNulls();
            notPublished.Select(u => u.PublishedOn).Should().AllBeEquivalentTo((DateTimeOffset?)null);

            // ACT again to publish remaining updates
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            updates = (await updatesInspectionRepository.GetAll()).ToList();
            updates.Count(u => u.IsPublished).Should().Be(totalCount);
            updates.Select(u => u.PublishedOn).Should().NotContainNulls();
        }

        // Importing donors creates new updates for publishing - this functionality is tested elsewhere.
        private async Task BuildAndImportDonors(int donorCount)
        {
            var donors = DonorBuilder.Build(donorCount).ToArray();
            var importFile = FileBuilder.WithDonors(donors).Build();
            await fileImporter.ImportDonorFile(importFile);
        }
    }
}
