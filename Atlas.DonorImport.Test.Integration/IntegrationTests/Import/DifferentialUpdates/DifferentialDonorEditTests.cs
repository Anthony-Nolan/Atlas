using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
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
    public class DifferentialDonorEditTests
    {
        private IMessageBatchPublisher<SearchableDonorUpdate> mockMessagePublisher;
        private INotificationSender mockNotificationSender;
        
        private const string DonorCodePrefix = "external-donor-code-";
        private IDonorInspectionRepository donorRepository;
        private IDonorFileImporter donorFileImporter;
        private List<Donor> InitialDonors;
        private const int InitialCount = 10;
        private readonly Builder<DonorImportFile> fileBuilder = DonorImportFileBuilder.NewWithoutContents;
        private Builder<DonorUpdate> donorEditBuilderForInitialDonors =>
            DonorUpdateBuilder.New
                .With(update => update.RecordId, InitialDonors.Select(donor => donor.ExternalDonorCode))
                .With(upd => upd.ChangeType, ImportDonorChangeType.Edit);

        private const string hla1 = "*01:01";
        private const string hla2 = "*01:02";
        private const string hla3 = "*01:03";
        private readonly ImportedHla hlaObject1 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(hla1).Build();
        private readonly ImportedHla hlaObject2 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(hla2).Build();
        private readonly ImportedHla hlaObject3 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(hla3).Build();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockNotificationSender = Substitute.For<INotificationSender>();
                var services = DependencyInjection.ServiceConfiguration.BuildServiceCollection();
                services.AddScoped(sp => mockNotificationSender);
                DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();
                
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Ensure any mocks set up for this test do not stick around.
            DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
        }

        [SetUp]
        public async Task SetUp()
        {
            //We want a new one of these every time, for ease of asserting calls.
            mockMessagePublisher = DependencyInjection.DependencyInjection.Provider.GetService<IMessageBatchPublisher<SearchableDonorUpdate>>();

            var newDonorUpdates =
                DonorUpdateBuilder.New
                    .WithRecordIdPrefix(DonorCodePrefix)
                    .With(donor => donor.ChangeType, ImportDonorChangeType.Create)
                    .With(donor => donor.Hla, new[] { hlaObject1, hlaObject2 })
                    .Build(InitialCount).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(newDonorUpdates).Build();

            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            InitialDonors = donorRepository.StreamAllDonors().ToList();
            InitialDonors.Should().HaveCount(InitialCount);
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockMessagePublisher.ClearReceivedCalls();
                mockNotificationSender.ClearReceivedCalls();
                DatabaseManager.ClearDatabases();
            });
        }

        [Test]
        public async Task ImportDonors_ForSingleEdits_RecordIsChangedInDatabase()
        {
            var donorEdit = donorEditBuilderForInitialDonors
                .With(donor => donor.Hla, hlaObject3)
                .Build();

            var donorEditFile = fileBuilder.WithDonors(donorEdit);

            //ACT
            await donorFileImporter.ImportDonorFile(donorEditFile);

            var updatedDonor = await donorRepository.GetDonor(donorEdit.RecordId);
            var unchangedDonorAtInsertion = InitialDonors.Skip(1).Take(1).Single();
            var unchangedDonor = await donorRepository.GetDonor(unchangedDonorAtInsertion.ExternalDonorCode);
            updatedDonor.A_1.Should().Be(hla3);
            unchangedDonor.Should().BeEquivalentTo(unchangedDonorAtInsertion);
        }

        [Test]
        public async Task ImportDonors_ForSingleEdits_WhereNoPertinentInfoChanged_RecordNotChangedInDatabase_NorSendMessages()
        {
            var donorEdit = donorEditBuilderForInitialDonors
                .With(donor => donor.Hla, hlaObject1)
                .Build();

            var donorEditFile = fileBuilder.WithDonors(donorEdit);

            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorEditFile);

            var updatedDonor = await donorRepository.GetDonor(donorEdit.RecordId);
            var unchangedDonorAtInsertion = InitialDonors.Take(1).Single();
            unchangedDonorAtInsertion.Should().BeEquivalentTo(updatedDonor);

            await mockMessagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        }

        [Test]
        public async Task ImportDonors_ForMultipleEdits_RecordsAreChangedInDatabase()
        {
            var donorEdit = donorEditBuilderForInitialDonors
                .With(donor => donor.Hla, hlaObject3)
                .Build(2).ToArray();

            var donorEditFile = fileBuilder.WithDonors(donorEdit);

            //ACT
            await donorFileImporter.ImportDonorFile(donorEditFile);

            var updatedDonor1 = await donorRepository.GetDonor(donorEdit[0].RecordId);
            var updatedDonor2 = await donorRepository.GetDonor(donorEdit[1].RecordId);
            updatedDonor1.A_1.Should().Be(hla3);
            updatedDonor2.A_2.Should().Be(hla3);
        }

        [Test]
        public async Task ImportDonors_ForMultipleEdits_WhereNoPertinentInfoChangedForSingleDonor_RecordsAreChangedInDatabaseForDonorWithPertinentInfoThatChanged()
        {
            var donorEdit = donorEditBuilderForInitialDonors
                .With(donor => donor.Hla, new[] {hlaObject1,  hlaObject3})
                .Build(2).ToArray();

            var donorEditFile = fileBuilder.WithDonors(donorEdit);

            //ACT
            await donorFileImporter.ImportDonorFile(donorEditFile);

            var unchangedDonorAtInsertion = InitialDonors.Take(1).Single();
            var updatedDonor1 = await donorRepository.GetDonor(donorEdit[0].RecordId);
            var updatedDonor2 = await donorRepository.GetDonor(donorEdit[1].RecordId);
            unchangedDonorAtInsertion.Should().BeEquivalentTo(updatedDonor1);
            updatedDonor2.A_2.Should().Be(hla3);
        }

        [Test]
        public async Task ImportDonors_ForEdits_SendsMessagesMatchingTheNewProperties_AndAtlasIds()
        {
            var donorEdit = donorEditBuilderForInitialDonors
                .With(donor => donor.Hla, new []{hlaObject3, hlaObject1})
                .Build(2).ToArray();

            var donorEditFile = fileBuilder.WithDonors(donorEdit);
            mockMessagePublisher.ClearReceivedCalls();
            var capturedUpdates = ConfigureCapturingOfUpdateMessageBatches();

            //ACT
            await donorFileImporter.ImportDonorFile(donorEditFile);

            var updatedDonor1 = await donorRepository.GetDonor(donorEdit[0].RecordId);
            var updatedDonor2 = await donorRepository.GetDonor(donorEdit[1].RecordId);
            capturedUpdates.Should().ContainSingle(message => (message.DonorId == updatedDonor1.AtlasId && message.SearchableDonorInformation.A_1 == hla3));
            capturedUpdates.Should().ContainSingle(message => (message.DonorId == updatedDonor2.AtlasId && message.SearchableDonorInformation.A_2 == hla1));
        }

        [Test]
        public async Task ImportDonors_ForEdits_IfRecordIsNotFound_Throws_AndDoesNotAffectExistingRecords_NorSendMessages()
        {
            var deletionCount = 4;
            var donorDeletes = donorEditBuilderForInitialDonors
                .With(update => update.RecordId, "Unknown")
                .Build(deletionCount).ToArray();

            var donorDeleteFile = fileBuilder.WithDonors(donorDeletes);
            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorDeleteFile);

            await mockNotificationSender.ReceivedWithAnyArgs(1).SendAlert(default, default, default, default);
            
            var unchangedDonors = donorRepository.StreamAllDonors().ToList();
            unchangedDonors.Should().BeEquivalentTo(InitialDonors);
            await mockMessagePublisher.DidNotReceive().BatchPublish(Arg.Is<ICollection<SearchableDonorUpdate>>(collection => collection.Any()));
        }

        [Test]
        public async Task ImportDonors_ForEdits_IfSomeRecordsAreNotFoundButOthersAre_Throws_AndDoesNotAffectExistingRecords_NorSendMessages()
        {
            var badEditBuilder = donorEditBuilderForInitialDonors.With(update => update.RecordId, "Unknown");

            var goodDonorEditUpdates = donorEditBuilderForInitialDonors.Build(4).ToArray();
            var badDonorEditUpdates = badEditBuilder.Build(3).ToArray();
            var mixedDonorUpdates = goodDonorEditUpdates.Union(badDonorEditUpdates).ToArray();

            var mixedDonorUpdateFile = fileBuilder.WithDonors(mixedDonorUpdates).Build();

            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(mixedDonorUpdateFile);

            await mockNotificationSender.ReceivedWithAnyArgs(1).SendAlert(default, default, default, default);

            var unchangedDonors = donorRepository.StreamAllDonors().ToList();
            unchangedDonors.Should().BeEquivalentTo(InitialDonors);
            await mockMessagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);

        }

        private List<SearchableDonorUpdate> ConfigureCapturingOfUpdateMessageBatches()
        {
            var capturedUpdates = new List<SearchableDonorUpdate>();
            mockMessagePublisher
                .When(client => client.BatchPublish(Arg.Any<ICollection<SearchableDonorUpdate>>()))
                .Do(clientCallArgs => capturedUpdates.AddRange(clientCallArgs.Arg<ICollection<SearchableDonorUpdate>>()));

            mockMessagePublisher.ClearReceivedCalls();
            return capturedUpdates;
        }
    }
}