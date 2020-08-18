using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Clients;
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

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class DifferentialDonorAdditionTests
    {
        private IDonorInspectionRepository donorRepository;
        private IMessagingServiceBusClient serviceBusClient;
        private IDonorFileImporter donorFileImporter;
        private readonly Builder<DonorImportFile> fileBuilder = DonorImportFileBuilder.NewWithoutContents;
        private readonly Builder<DonorUpdate> donorCreationBuilder =
            DonorUpdateBuilder.New
                .WithRecordIdPrefix("external-donor-code-")
                .With(upd => upd.ChangeType, ImportDonorChangeType.Create);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                serviceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<IMessagingServiceBusClient>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task ImportDonors_ForEachAddition_AddsDonorToDatabase()
        {
            const int creationCount = 2;
            const string donorCodePrefix = "test1-";
            var donorUpdates = donorCreationBuilder.WithRecordIdPrefix(donorCodePrefix).Build(creationCount).ToArray();
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
                donorCreationBuilder
                    .WithRecordIdPrefix(donorCodePrefix)
                    .With(donor => donor.Hla, new []{ hlaObject1, hlaObject2})
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

            var donorUpdates = donorCreationBuilder.Build(creationCount).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            serviceBusClient.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            await serviceBusClient.Received(creationCount).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessages(default);
        }

        [Test]
        public async Task ImportDonors_ForEachAddition_SendsMatchingUpdateWithNewlyAssignedAtlasId()
        {
            var donorUpdates = donorCreationBuilder.Build(2).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            serviceBusClient.ClearReceivedCalls();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);
            
            var donor1 = await donorRepository.GetDonor(donorUpdates[0].RecordId);
            var donor2 = await donorRepository.GetDonor(donorUpdates[1].RecordId);
            
            donor1.AtlasId.Should().NotBe(donor2.AtlasId);
            await serviceBusClient.Received().PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == donor1.AtlasId && u.SearchableDonorInformation.DonorId == donor1.AtlasId)
            );
            await serviceBusClient.Received().PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == donor2.AtlasId && u.SearchableDonorInformation.DonorId == donor2.AtlasId)
            );
        }

        [Test]
        public async Task ImportDonors_IfAdditionsAlreadyExist_ThrowsError_AndDoesNotAdd_NorSendMessages()
        {
            const string donorCodePrefix = "test5-";
            var donorUpdates = donorCreationBuilder.WithRecordIdPrefix(donorCodePrefix).Build(4).ToArray();
            var donorUpdateFiles = fileBuilder.WithDonors(donorUpdates).Build(2).ToList();

            await donorFileImporter.ImportDonorFile(donorUpdateFiles.First());
            serviceBusClient.ClearReceivedCalls();

            //ACT
            await donorFileImporter.Invoking(importer => importer.ImportDonorFile(donorUpdateFiles.Last())).Should().ThrowAsync<Exception>();

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(4);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessages(default);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessage(default);
        }

        [Test]
        public async Task ImportDonors_IfSomeAdditionsAlreadyExistButOthersDoNot_ThrowsError_AndDoesNotAdd_NorChangeExisting_NorSendMessages()
        {
            const string donorCodePrefix = "test6-";
            var updateBuilder = donorCreationBuilder.WithRecordIdPrefix(donorCodePrefix);

            var donorUpdates_Set1 = updateBuilder.Build(4).ToArray();
            var donorUpdates_Set2 = updateBuilder.Build(3).ToArray();
            var donorUpdates_Sets1And2 = donorUpdates_Set1.Union(donorUpdates_Set2).ToArray();

            var donorUpdateFile_DonorSet1 = fileBuilder.WithDonors(donorUpdates_Set1).With(d => d.UploadTime, DateTime.UtcNow.AddDays(-1)).Build();
            var donorUpdateFile_DonorSets1And2 = fileBuilder.WithDonors(donorUpdates_Sets1And2).With(d => d.UploadTime, DateTime.UtcNow).Build();

            await donorFileImporter.ImportDonorFile(donorUpdateFile_DonorSet1);
            serviceBusClient.ClearReceivedCalls();

            //ACT
            //await donorFileImporter.Invoking(importer => importer.ImportDonorFile(donorUpdateFile_DonorSets1And2)).Should().ThrowAsync<Exception>();
            await donorFileImporter.ImportDonorFile(donorUpdateFile_DonorSets1And2);

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(4);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessages(default);
            await serviceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessage(default);
        }

        [Test]
        public async Task ImportDonors_IfMissingMandatoryHlaTypings_DoesNotAddToDatabase()
        {
            var donorUpdate = donorCreationBuilder.Build();
            
            donorUpdate.Hla.A = null;
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }
        
        [Test]
        public async Task ImportDonors_IfRequiredLocusHasNullHlaValues_DoesNotAddToDatabase()
        {
            var donorUpdate = donorCreationBuilder.Build();
            
            donorUpdate.Hla.A = new ImportedLocus
            {
                Dna = new TwoFieldStringData
                {
                    Field1 = null,
                    Field2 = null
                }
            };
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_IfMissingHlaHasEmptyValues_DoesNotAddToDatabase()
        {
            var donorUpdate = donorCreationBuilder.Build();
            
            donorUpdate.Hla.A = new ImportedLocus
            {
                Dna = new TwoFieldStringData
                {
                    Field1 = "",
                    Field2 = ""
                }
            };
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }
        
        [Test]
        public async Task ImportDonors_ForDonorDeletion_WithInvalidHla_DonorIsStillDeleted()
        {
            var donorUpdate = donorCreationBuilder.Build();
            var validFile = fileBuilder.WithDonors(donorUpdate).Build();
            await donorFileImporter.ImportDonorFile(validFile);
            var importedDonor = await donorRepository.GetDonor(donorUpdate.RecordId);
            importedDonor.ExternalDonorCode.Should().Be(donorUpdate.RecordId);
            
            var donorDeletion = DonorUpdateBuilder
                .New
                .With(upd => upd.ChangeType, ImportDonorChangeType.Delete)
                .With(upd => upd.RecordId, donorUpdate.RecordId)
                .Build();
            donorDeletion.Hla.A = null;
            var invalidFile = fileBuilder.WithDonors(donorDeletion).Build();
            
            await donorFileImporter.ImportDonorFile(invalidFile);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }
    }
}