using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
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
        private INotificationSender mockNotificationsSender;

        private IDonorInspectionRepository donorRepository;
        private IPublishableDonorUpdatesInspectionRepository updatesInspectionRepository;
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
                updatesInspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IPublishableDonorUpdatesInspectionRepository>();
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
                    .With(donor => donor.Hla, new[] { hlaObject1, hlaObject2 })
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
        public async Task ImportDonors_ForEachAdditions_SavesPublishableDonorUpdate()
        {
            const int creationCount = 3;

            var donorUpdates = DonorCreationBuilder.Build(creationCount).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            var updateCountBeforeImport = await updatesInspectionRepository.Count();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            var updatesCountAfterImport = await updatesInspectionRepository.Count();

            (updatesCountAfterImport - updateCountBeforeImport).Should().Be(creationCount);
        }

        [Test]
        public async Task ImportDonors_ForEachAddition_SavesPublishableDonorUpdateWithNewlyAssignedAtlasId()
        {
            var donorUpdates = DonorCreationBuilder.Build(2).ToArray();
            var donorUpdateFile = fileBuilder.WithDonors(donorUpdates).Build();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile);

            var donor1 = await donorRepository.GetDonor(donorUpdates[0].RecordId);
            var donor2 = await donorRepository.GetDonor(donorUpdates[1].RecordId);
            var update1 = (await updatesInspectionRepository.Get(new[] { donor1.AtlasId }, true)).SingleOrDefault();
            var update2 = (await updatesInspectionRepository.Get(new[] { donor2.AtlasId }, true)).SingleOrDefault();

            // ASSERT
            donor1.AtlasId.Should().NotBe(donor2.AtlasId);
            update1.Should().NotBeNull();
            update2.Should().NotBeNull();
            update1?.ToSearchableDonorUpdate().SearchableDonorInformation.DonorId.Should().Be(donor1.AtlasId);
            update2?.ToSearchableDonorUpdate().SearchableDonorInformation.DonorId.Should().Be(donor2.AtlasId);
        }

        [Test]
        public async Task ImportDonors_IfAdditionsAlreadyExist_ThrowsError_AndDoesNotAdd_NorSavesUpdate()
        {
            const string donorCodePrefix = "test5-";
            var donorUpdates = DonorCreationBuilder.WithRecordIdPrefix(donorCodePrefix).Build(4).ToArray();
            var donorUpdateFiles = fileBuilder.WithDonors(donorUpdates).Build(2).ToList();

            await donorFileImporter.ImportDonorFile(donorUpdateFiles.First());

            var updateCountBeforeSecondImport = await updatesInspectionRepository.Count();

            //ACT
            await donorFileImporter.Invoking(importer => importer.ImportDonorFile(donorUpdateFiles.Last())).Should().ThrowAsync<Exception>();

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(4);

            var updateCountAfterSecondImport = await updatesInspectionRepository.Count();
            updateCountAfterSecondImport.Should().Be(updateCountBeforeSecondImport);
        }

        [Test]
        public async Task ImportDonors_IfSomeAdditionsAlreadyExistButOthersDoNot_ThrowsError_AndDoesNotAdd_NorChangeExisting_NorSavesUpdates()
        {
            const string donorCodePrefix = "test6-";
            var updateBuilder = DonorCreationBuilder.WithRecordIdPrefix(donorCodePrefix);

            var donorUpdates_Set1 = updateBuilder.Build(4).ToArray();
            var donorUpdates_Set2 = updateBuilder.Build(3).ToArray();
            var donorUpdates_Sets1And2 = donorUpdates_Set1.Union(donorUpdates_Set2).ToArray();

            var donorUpdateFile_DonorSet1 = fileBuilder.WithDonors(donorUpdates_Set1).With(d => d.UploadTime, DateTime.UtcNow.AddDays(-1)).Build();
            var donorUpdateFile_DonorSets1And2 = fileBuilder.WithDonors(donorUpdates_Sets1And2).With(d => d.UploadTime, DateTime.UtcNow).Build();

            await donorFileImporter.ImportDonorFile(donorUpdateFile_DonorSet1);

            var updateCountBeforeSecondImport = await updatesInspectionRepository.Count();

            //ACT
            await donorFileImporter.ImportDonorFile(donorUpdateFile_DonorSets1And2);

            await mockNotificationsSender.ReceivedWithAnyArgs(1).SendAlert(default, default, default, default);

            donorRepository.StreamAllDonors().Where(donor => donor.ExternalDonorCode.StartsWith(donorCodePrefix)).Should().HaveCount(4);

            var updateCountAfterSecondImport = await updatesInspectionRepository.Count();
            updateCountAfterSecondImport.Should().Be(updateCountBeforeSecondImport);
        }
    }
}