using System;
using System.Threading.Tasks;
using Atlas.Common.Notifications;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
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

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class UploadOrderTests
    {
        private INotificationSender mockNotificationSender;
        
        private Builder<DonorUpdate> createUpdateBuilder;
        private Builder<DonorUpdate> editUpdateBuilder;
        private Builder<DonorUpdate> deleteUpdateBuilder;
        private Builder<DonorUpdate> upsertUpdateBuilder;
        private Builder<DonorImportFile> fileBuilder;
        private IDonorFileImporter donorFileImporter;
        private IDonorInspectionRepository donorRepository;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            createUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Create);
            editUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Edit);
            deleteUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Delete);
            upsertUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Upsert);
            fileBuilder = DonorImportFileBuilder.NewWithoutContents;
            
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockNotificationSender = Substitute.For<INotificationSender>();
                var services = DependencyInjection.ServiceConfiguration.BuildServiceCollection();
                services.AddScoped(sp => mockNotificationSender);
                DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();
                
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                mockNotificationSender = DependencyInjection.DependencyInjection.Provider.GetService<INotificationSender>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Ensure any mocks set up for this test do not stick around.
            DependencyInjection.DependencyInjection.BackingProvider = DependencyInjection.ServiceConfiguration.CreateProvider();
        }

        [TearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                mockNotificationSender.ClearReceivedCalls();
                DatabaseManager.ClearDatabases();
            });
        }

        [Test]
        public async Task DonorImportOrder_CreateThenCreateImport_PostsError()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile1 = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = create
            var file2Name = "file-2";
            var createFile2 = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);
            
            // Import File 1, check it is imported.
            await donorFileImporter.ImportDonorFile(createFile1);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
            
            // import File 2, it should post error and donor remain unchanged.
            await donorFileImporter.ImportDonorFile(createFile2);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }
        
        [Test]
        public async Task DonorImportOrder_CreateThenCreateImportOutOfOrder_PostsError()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile1 = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = create
            var file2Name = "file-2";
            var createFile2 = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);
            
            // Import File 2, check it is imported.
            await donorFileImporter.ImportDonorFile(createFile2);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);
            
            // import File 1, it should post error and donor remain unchanged.
            await donorFileImporter.ImportDonorFile(createFile1);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }
        
        [Test]
        public async Task DonorImportOrder_CreateThenUpdate_UpdatesDonor()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, check it is imported.
            await donorFileImporter.ImportDonorFile(createFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
                        
            // Import File 2, it should update the donor
            await donorFileImporter.ImportDonorFile(editFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }
        
        [Test]
        public async Task DonorImportOrder_CreateThenUpdateOutOfOrder_PostsErrorAndOutOfDateDonor()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, Expect error and no donor
            await donorFileImporter.ImportDonorFile(editFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
                        
            // Import File 1, Expect out of date donor
            await donorFileImporter.ImportDonorFile(createFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }

        [Test]
        public async Task DonorImportOrder_CreateThenUpsert_UpdatesDonor()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, check it is imported
            await donorFileImporter.ImportDonorFile(createFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);

            // Import File 2, it should update the donor
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_CreateThenUpsertOutOfOrder_DonorCreatedAndPostsError()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, check it is imported
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // Import File 1, it should post error and donor remain unchanged
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_CreateThenDelete_DeletesDonor()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Delete
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 2);
            
            // import first file, check it is imported.
            await donorFileImporter.ImportDonorFile(createFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
            

            // Import File 2, it should delete the donor.
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }
        
        [Test]
        public async Task DonorImportOrder_CreateThenDeleteOutOfOrder_ErrorsWithOutOfDateDonor()
        {
            // File 1 = Create
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Delete
            var file2Name = "file-2";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file2Name, 2);
            
            // Import File 2, expect no error and no donor
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
            
            // import File 1, check it is imported (even though out of date)
            await donorFileImporter.ImportDonorFile(createFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
        }
        
        [Test]
        public async Task DonorImportOrder_EditThenCreateWithoutPreExistingDonor_PostsErrorAndCreates()
        {
            // File 1 = Edit
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            
            // import File 1, expect error and no donor.
            await donorFileImporter.ImportDonorFile(editFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
            

            // import File 2, Donor gets created.
            await donorFileImporter.ImportDonorFile(createFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }
        
        [Test]
        public async Task DonorImportOrder_EditThenCreateWithoutPreExistingDonorOutOfOrder_CreatesAndDiscardsEdit()
        {
            // File 1 = Edit
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Donor gets created.
            await donorFileImporter.ImportDonorFile(createFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
            
            // import File 1, expect no change
            await donorFileImporter.ImportDonorFile(editFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);
            
        }
        
        [Test]
        public async Task DonorImportOrder_EditThenCreateWithExistingDonor_Creates()
        {
            var donorExternalCode = "1";
            
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            
            // import File 1, expect donor update
            await donorFileImporter.ImportDonorFile(editFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
            

            // import File 2, Error and unchanged donor
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }
        
        [Test]
        public async Task DonorImportOrder_EditThenCreateWithExistingDonorOutOfOrder_Creates()
        {
            var donorExternalCode = "1";
            var initialFileName = "file-0";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, initialFileName, 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Posts Error, donor is unchanged.
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(initialFileName);
            
            // import File 1, Updates file
            await donorFileImporter.ImportDonorFile(editFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
        }
        
        [Test]
        public async Task DonorImportOrder_UpdateThenUpdate_Updates()
        {
            var donorExternalCode = "1";
            
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var editFile2 = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Donor gets Updated
            await donorFileImporter.ImportDonorFile(editFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
            
            // import File 2, Donor gets updated again
            await donorFileImporter.ImportDonorFile(editFile2);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }
        
        [Test]
        public async Task DonorImportOrder_UpdateThenUpdateOutOfOrder_RejectsSecondUpdate()
        {
            var donorExternalCode = "1";
            
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var editFile2 = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Donor gets Updated
            await donorFileImporter.ImportDonorFile(editFile2);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);
            
            // import File 1, Donor does not update
            await donorFileImporter.ImportDonorFile(editFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpdateThenUpsertWithoutPreExistingDonor_PostsErrorAndCreates()
        {
            // File 1 = Edit
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, expect error and no donor.
            await donorFileImporter.ImportDonorFile(editFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();

            // import File 2, Donor gets created.
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpdateThenUpsertWithoutPreExistingDonorOutOfOrder_CreatesAndDiscardsEdit()
        {
            // File 1 = Edit
            var donorExternalCode = "1";
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Donor gets created.
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // import File 1, Donor does not update.
            await donorFileImporter.ImportDonorFile(editFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpdateThenUpsertWithExistingDonor_Updates()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Donor gets Updated
            await donorFileImporter.ImportDonorFile(editFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);

            // import File 2, Donor gets updated again
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpdateThenUpsertWithExistingDonorOutOfOrder_UpdatesAndRejectsSecondUpdate()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Donor gets Updated
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // import File 1, Donor does not update
            await donorFileImporter.ImportDonorFile(editFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpdateThenDelete_Deletes()
        {
            var donorExternalCode = "1";
            
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Delete
            var file2Name = "file-2";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Donor gets Updated
            await donorFileImporter.ImportDonorFile(editFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
            
            // import File 2, Donor gets deleted
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
        }
        
        [Test]
        public async Task DonorImportOrder_UpdateThenDeleteOutOfOrder_DeletesWithError()
        {
            var donorExternalCode = "1";
            
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Edit
            var file1Name = "file-1";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Delete
            var file2Name = "file-2";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Donor gets Deleted
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
            
            // import File 1, Error with no donor
            await donorFileImporter.ImportDonorFile(editFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenCreateWithoutExistingDonor_DonorCreatedThenPostsError()
        {
            var donorExternalCode = "1";

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, Donor created
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);

            // Import File 2, Error and donor unchanged
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenCreateWithoutExistingDonorOutOfOrder_DonorCreatedThenDiscardsChanges()
        {
            var donorExternalCode = "1";

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, Donor created
            await donorFileImporter.ImportDonorFile(createFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // Import File 1, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenCreateWithExistingDonor_DonorUpdatedThenPostsError()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);
            
            //File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);
            
            // Import File 2, Error and donor unchanged
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenCreateWithExistingDonorOutOfOrder_PostsErrorThenDonorUpdated()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var initialFileName = "file-0";
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, initialFileName, 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, Error and donor unchanged
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(initialFileName);

            // Import File 1, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenUpdate_Updates()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Update
            var file2Name = "file-2";
            var updateFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);

            // Import File 2, Donor updated
            await donorFileImporter.ImportDonorFile(updateFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenUpdateOutOfOrder_RejectsSecondUpdate()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Update
            var file2Name = "file-2";
            var updateFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, Donor updated
            await donorFileImporter.ImportDonorFile(updateFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // Import File 1, Donor does not update
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenUpsert_Updates()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile1 = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile2 = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile1);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);

            // Import File 2, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile2);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenUpsertOutOfOrder_RejectsSecondUpsert()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile1 = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile2 = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile2);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // Import File 1, Donor does not update
            await donorFileImporter.ImportDonorFile(upsertFile1);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenDelete_Deletes()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Delete
            var file2Name = "file-2";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 1, Donor updated
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file1Name);

            // Import File 2, Donor deleted
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }

        [Test]
        public async Task DonorImportOrder_UpsertThenDeleteOutOfOrder_DeletesThenCreatesDonor()
        {
            var donorExternalCode = "1";

            // File 0 - donor is already created
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Upsert
            var file1Name = "file-1";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file1Name, 1);

            //File 2 = Delete
            var file2Name = "file-2";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file2Name, 2);

            // Import File 2, Donor deleted
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();

            // Import File 1, Donor created
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file1Name);
        }

        [Test]
        public async Task DonorImportOrder_DeleteThenCreate_DonorCreated()
        {
            var donorExternalCode = "1";
            
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Donor gets Deleted
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
            
            // import File 2, Donor gets Created
            await donorFileImporter.ImportDonorFile(createFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }
        
        [Test]
        public async Task DonorImportOrder_DeleteThenCreateOutOfOrder_DonorDeletedWithError()
        {
            var donorExternalCode = "1";
            var preExitingFileName = "file-0";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, preExitingFileName, 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Create
            var file2Name = "file-2";
            var createFile = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Error
            await donorFileImporter.ImportDonorFile(createFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(preExitingFileName);
            
            // import File 1, Donor gets Deleted
            
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }
        
        [Test]
        public async Task DonorImportOrder_DeleteThenEdit_DonorDeletedWithError()
        {
            var donorExternalCode = "1";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Deletes
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
            
            // import File 2, Error, no Donor
            await donorFileImporter.ImportDonorFile(editFile);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }
        
        [Test]
        public async Task DonorImportOrder_DeleteThenEditOutOfOrder_DonorNotDeleted()
        {
            var donorExternalCode = "1";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var editFile = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Updates
            await donorFileImporter.ImportDonorFile(editFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);
            
            // import File 1, Donor remains
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_DeleteThenUpsert_DonorCreated()
        {
            var donorExternalCode = "1";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Deletes
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();

            // import File 2, Donor created
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_DeleteThenUpsertOutOfOrder_DonorNotDeleted()
        {
            var donorExternalCode = "1";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);

            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);

            // File 2 = Upsert
            var file2Name = "file-2";
            var upsertFile = CreateDonorImportFile(upsertUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Upsert updates donor
            await donorFileImporter.ImportDonorFile(upsertFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.UpdateFile.Should().Be(file2Name);

            // import File 1, Donor remains
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.UpdateFile.Should().Be(file2Name);
        }

        [Test]
        public async Task DonorImportOrder_DeleteThenDelete_DonorDeletedWithError()
        {
            var donorExternalCode = "1";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Edit
            var file2Name = "file-2";
            var deleteFile2 = CreateDonorImportFile(editUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 1, Deletes
            await donorFileImporter.ImportDonorFile(deleteFile);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
            
            // import File 2, Error, still no Donor
            await donorFileImporter.ImportDonorFile(deleteFile2);
            await mockNotificationSender.ReceivedWithAnyArgs().SendAlert(default, default, default, default);
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }
        
        [Test]
        public async Task DonorImportOrder_DeleteThenDeleteOutOfOrder_DonorDeleted()
        {
            var donorExternalCode = "1";
            // File 0 - donor is already created.
            var existingDonor = CreateDonorImportFile(createUpdateBuilder, donorExternalCode, "not-applicable", 0);
            await donorFileImporter.ImportDonorFile(existingDonor);
            
            // File 1 = Delete
            var file1Name = "file-1";
            var deleteFile = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file1Name, 1);
            
            // File 2 = Delete
            var file2Name = "file-2";
            var deleteFile2 = CreateDonorImportFile(deleteUpdateBuilder, donorExternalCode, file2Name, 2);

            // import File 2, Deletes
            await donorFileImporter.ImportDonorFile(deleteFile2);
            var result1 = await donorRepository.GetDonor(donorExternalCode);
            result1.Should().BeNull();
            
            // import File 1, No Error still no Donor
            await donorFileImporter.ImportDonorFile(deleteFile);
            // Same problem as above
            var result2 = await donorRepository.GetDonor(donorExternalCode);
            result2.Should().BeNull();
        }
        
        private DonorImportFile CreateDonorImportFile(Builder<DonorUpdate> builder, string externalCode, string fileName, int order)
        {
            // We want to ensure each donor import is unique so we use order to determine HLA - order should be unique within each test.
            var donor1 = builder
                .With(d => d.RecordId, externalCode)
                .WithHomozygousHlaAt(Locus.A, $"*01:{order}").Build();
            var edit = new[]
            {
                donor1
            };
            
            return fileBuilder.WithDonors(edit)
                .With(f => f.FileLocation, fileName)
                .With(f => f.UploadTime, DateTime.UtcNow.AddDays(order - 10))
                .Build();
        }
    }
}