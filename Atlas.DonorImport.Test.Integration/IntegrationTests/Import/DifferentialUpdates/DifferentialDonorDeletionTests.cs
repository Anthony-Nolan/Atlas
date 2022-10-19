using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.DifferentialUpdates
{
    [TestFixture]
    public class DifferentialDonorDeletionTests
    {
        private IMessageBatchPublisher<SearchableDonorUpdate> messagePublisher;

        private IDonorInspectionRepository donorRepository;
        private IDonorFileImporter donorFileImporter;

        private List<Donor> initialDonors;
        private const int InitialCount = 10;
        private readonly Builder<DonorImportFile> fileBuilder = DonorImportFileBuilder.NewWithoutContents;

        private static Builder<DonorUpdate> DonorDeletionBuilder =>
            DonorUpdateBuilder.New
                .WithRecordIdPrefix("external-donor-code-")
                .With(upd => upd.ChangeType, ImportDonorChangeType.Delete);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                messagePublisher = DependencyInjection.DependencyInjection.Provider.GetService<IMessageBatchPublisher<SearchableDonorUpdate>>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Ensure any mocks set up for this test do not stick around.
            DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
        }

        [SetUp]
        public async Task ImportInitialDonors()
        {
            var donorCreationUpdates =
                DonorUpdateBuilder.New
                    .With(upd => upd.ChangeType, ImportDonorChangeType.Create)
                    .Build(InitialCount).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorCreationUpdates);

            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            initialDonors = donorRepository.StreamAllDonors().ToList();
            initialDonors.Should().HaveCount(InitialCount);
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                messagePublisher.ClearReceivedCalls();
                DatabaseManager.ClearDatabases();
            });
        }

        [Test]
        public async Task ImportDonors_ForDeletions_RecordsAreRemovedFromDatabase()
        {
            const int deletionCount = 3;
            var donorDeletes = DonorDeletionBuilder
                .With(update => update.RecordId, initialDonors.Select(d => d.ExternalDonorCode).ToList())
                .Build(deletionCount).ToArray();

            var donorDeleteFile = fileBuilder.WithDonors(donorDeletes);

            //ACT
            await donorFileImporter.ImportDonorFile(donorDeleteFile);

            var updatedDonors = donorRepository.StreamAllDonors().ToList();
            updatedDonors.Should().HaveCount(InitialCount - deletionCount);
        }

        [Test]
        public async Task ImportDonors_ForDeletionsIfRecordsAreNotFound_DoesNotThrow_AndDoesNotAffectExistingRecords_AndDoesNotSendMessages()
        {
            const int deletionCount = 4;
            var donorDeletes = DonorDeletionBuilder
                .With(update => update.RecordId, "Unknown")
                .Build(deletionCount).ToArray();

            var donorDeleteFile = fileBuilder.WithDonors(donorDeletes);
            messagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.Invoking(importer => importer.ImportDonorFile(donorDeleteFile)).Should().NotThrowAsync();

            var remainingDonors = donorRepository.StreamAllDonors().ToList();
            remainingDonors.Should().HaveCount(InitialCount);
            await messagePublisher.DidNotReceive()
                .BatchPublish(Arg.Is<ICollection<SearchableDonorUpdate>>(collection => collection.Any()));
        }

        [Test]
        public async Task
            ImportDonors_ForDeletions_WithMixOfRecordsFoundAndNotFound_FoundRecordsAreDeletedFromDatabase_AndSendsMessagesMatchingTheDeletedAtlasIds()
        {
            const int goodDeletesCount = 2;
            const int badDeletesCount = 6;

            var (donorMixedDeleteFile, goodDeleteAtlasIds) = GenerateMixedDeletionFileWithMatchingAtlasIds(goodDeletesCount, badDeletesCount);
            var capturedUpdates = ConfigureCapturingOfUpdateMessages();

            //ACT
            await donorFileImporter.ImportDonorFile(donorMixedDeleteFile);

            var remainingDonors = donorRepository.StreamAllDonors().ToList();
            remainingDonors.Should().HaveCount(InitialCount - goodDeletesCount);
            await messagePublisher.ReceivedWithAnyArgs(1).BatchPublish(default);
            capturedUpdates.Should().HaveCount(goodDeletesCount);
            capturedUpdates.Select(update => update.DonorId).Should().BeEquivalentTo(goodDeleteAtlasIds);
        }

        private (DonorImportFile, List<int>) GenerateMixedDeletionFileWithMatchingAtlasIds(int goodDeletesCount, int badDeletesCount)
        {
            var goodDeletes = DonorDeletionBuilder
                .With(update => update.RecordId, initialDonors.Select(d => d.ExternalDonorCode).ToList())
                .Build(goodDeletesCount).ToList();
            var badDeletes = DonorDeletionBuilder.With(update => update.RecordId, "Unknown").Build(badDeletesCount);
            var goodDeleteAtlasIds =
                goodDeletes.Select(delete => donorRepository.GetDonor(delete.RecordId).Result.AtlasId)
                    .ToList(); // Note reification must occur before we Import the File!

            var donorMixedDeleteFile = fileBuilder.WithDonors(goodDeletes.Union(badDeletes).ToArray()).Build();

            return (donorMixedDeleteFile, goodDeleteAtlasIds);
        }

        private List<SearchableDonorUpdate> ConfigureCapturingOfUpdateMessages()
        {
            var capturedUpdates = new List<SearchableDonorUpdate>();
            messagePublisher
                .When(client => client.BatchPublish(Arg.Any<ICollection<SearchableDonorUpdate>>()))
                .Do(clientCallArgs => capturedUpdates.AddRange(clientCallArgs.Arg<ICollection<SearchableDonorUpdate>>()));

            messagePublisher.ClearReceivedCalls();
            return capturedUpdates;
        }
    }
}