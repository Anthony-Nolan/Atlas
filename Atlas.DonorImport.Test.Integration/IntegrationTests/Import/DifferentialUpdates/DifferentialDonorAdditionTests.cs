using System;
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

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.DifferentialUpdates
{
    [TestFixture]
    public class DifferentialDonorAdditionTests
    {
        private IMessageBatchPublisher<SearchableDonorUpdate> mockMessagePublisher;
        private INotificationSender mockNotificationsSender;

        private IDonorInspectionRepository donorRepository;
        private IDonorFileImporter donorFileImporter;
        private readonly Builder<DonorImportFile> fileBuilder = DonorImportFileBuilder.NewWithoutContents;

        private Builder<DonorUpdate> DonorCreationBuilder =>
            DonorUpdateBuilder.New
                .WithRecordIdPrefix("external-donor-code-")
                .With(upd => upd.ChangeType, ImportDonorChangeType.Create);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockNotificationsSender = Substitute.For<INotificationSender>();
                var services = DependencyInjection.ServiceConfiguration.BuildServiceCollection();
                services.AddScoped(sp => mockNotificationsSender);
                DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();

                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                mockMessagePublisher = DependencyInjection.DependencyInjection.Provider.GetService<IMessageBatchPublisher<SearchableDonorUpdate>>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
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
            mockNotificationsSender.ClearReceivedCalls();
            mockMessagePublisher.ClearReceivedCalls();
        }

        [Test]
        public async Task ImportDonors_ForEachAddition_AddsDonorToDatabase()
        {
            const int creationCount = 2;
            const string donorCodePrefix = "test1-";
            var donorUpdates = DonorCreationBuilder.WithRecordIdPrefix(donorCodePrefix).Build(creationCount).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(creationCount);
        }

        [Test]
        public async Task ImportDonors_ForAnAddition_HlaDataIsRecordedOnDonorInDatabase()
        {
            const string hla1 = "*01:01";
            const string hla2 = "*01:02";
            var hlaObject1 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(hla1).Build();
            var hlaObject2 = HlaBuilder.New.WithHomozygousMolecularHlaAtAllLoci(hla2).Build();

            const string donorCodePrefix = "test2-";
            var donorUpdates =
                DonorCreationBuilder
                    .WithRecordIdPrefix(donorCodePrefix)
                    .With(donor => donor.Hla, new[] {hlaObject1, hlaObject2})
                    .Build(2).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            var hlasInDb = donorRepository.StreamAllDonors()
                .Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix))
                .Select(donor => donor.A_1)
                .ToList();
            hlasInDb.Should().BeEquivalentTo(hla1, hla2);
        }

        [Test]
        public async Task ImportDonors_ForEachAdditions_SendsMatchingMessage()
        {
            const int creationCount = 3;

            var donorUpdates = DonorCreationBuilder.Build(creationCount).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            await mockMessagePublisher
                .Received(1)
                .BatchPublish(Arg.Is<List<SearchableDonorUpdate>>(messages => messages.Count == creationCount));
        }

        [Test]
        public async Task ImportDonors_ForEachAddition_SendsMatchingUpdateWithNewlyAssignedAtlasId()
        {
            var donorUpdates = DonorCreationBuilder.Build(2).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            var donor1 = await donorRepository.GetDonor(donorUpdates[0].RecordId);
            var donor2 = await donorRepository.GetDonor(donorUpdates[1].RecordId);

            donor1.AtlasId.Should().NotBe(donor2.AtlasId);
            await mockMessagePublisher.Received().BatchPublish(Arg.Is<List<SearchableDonorUpdate>>(messages =>
                messages.Any(u => u.DonorId == donor1.AtlasId && u.SearchableDonorInformation.DonorId == donor1.AtlasId)
                && messages.Any(u => u.DonorId == donor2.AtlasId && u.SearchableDonorInformation.DonorId == donor2.AtlasId))
            );
        }

        [Test]
        public async Task ImportDonors_IfAdditionsAlreadyExist_ThrowsError_AndDoesNotAdd_NorSendMessages()
        {
            const string donorCodePrefix = "test5-";
            var donorUpdates = DonorCreationBuilder.WithRecordIdPrefix(donorCodePrefix).Build(4).ToArray();
            var donorUpdateFiles = fileBuilder.WithDonors(donorUpdates).Build(2).ToList();

            await donorFileImporter.ImportDonorFile(donorUpdateFiles.First());
            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.Invoking(importer => importer.ImportDonorFile(donorUpdateFiles.Last())).Should().ThrowAsync<Exception>();

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(4);
            await mockMessagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        }

        [Test]
        public async Task ImportDonors_IfSomeAdditionsAlreadyExistButOthersDoNot_ThrowsError_AndDoesNotAdd_NorChangeExisting_NorSendMessages()
        {
            const string donorCodePrefix = "test6-";
            var updateBuilder = DonorCreationBuilder.WithRecordIdPrefix(donorCodePrefix);

            var donorUpdates_Set1 = updateBuilder.Build(4).ToArray();
            var donorUpdates_Set2 = updateBuilder.Build(3).ToArray();
            var donorUpdates_Sets1And2 = donorUpdates_Set1.Union(donorUpdates_Set2).ToArray();

            var donorUpdateFile_DonorSet1 = fileBuilder.WithDonors(donorUpdates_Set1).With(d => d.UploadTime, DateTime.UtcNow.AddDays(-1)).Build();
            var donorUpdateFile_DonorSets1And2 = fileBuilder.WithDonors(donorUpdates_Sets1And2).With(d => d.UploadTime, DateTime.UtcNow).Build();

            await donorFileImporter.ImportDonorFile(donorUpdateFile_DonorSet1);
            mockMessagePublisher.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile_DonorSets1And2);

            await mockNotificationsSender.ReceivedWithAnyArgs(1).SendAlert(default, default, default, default);

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(4);
            await mockMessagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        }
    }
}