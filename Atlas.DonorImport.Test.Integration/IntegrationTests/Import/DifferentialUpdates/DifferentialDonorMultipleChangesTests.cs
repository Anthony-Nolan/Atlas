using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.DifferentialUpdates
{
    /* ===============================================================
     * ===============================================================
     * ====                README                                 ====
     * ====     This test is a small snapshot test.               ====
     * ====     It applies a miscellany of changes to some        ====
     * ====     DB records, and checks that they end up looking   ====
     * ====     the way we expect them to look.                   ====
     * ====                                                       ====
     * ====     If this test ever fails then you should create    ====
     * ====     a dedicated test elsewhere to cover the failure   ====
     * ====     mode explicitly!                                  ====
     * ====                                                       ====
     * ====     The test is somewhat tricky to follow, which is   ====
     * ====     why in each block of changes we retain a note     ====
     * ====     of the last change made to the *other* records    ====
     * ====                                                       ====
     * ===============================================================
     * ===============================================================
     */
    [TestFixture]
    public class DifferentialDonorMultipleChangesTests
    {
        private IDonorInspectionRepository donorRepository;
        private IDonorFileImporter donorFileImporter;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            });
        }

        [TearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task ImportDonors_ForMiscellaneousFiles_FinalDatabaseStateIsExpected()
        {
            var createUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Create);
            var editUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Edit);
            var deleteUpdateBuilder = DonorUpdateBuilder.New.With(update => update.ChangeType, ImportDonorChangeType.Delete);
            var fileBuilder = DonorImportFileBuilder.NewWithoutContents;

            var create = new[]
            {
                createUpdateBuilder.With(d => d.RecordId, "1").WithHomozygousHlaAt(Locus.A, "*01:01").Build(),
                createUpdateBuilder.With(d => d.RecordId, "2").WithHomozygousHlaAt(Locus.A, "*01:02").Build(),
                createUpdateBuilder.With(d => d.RecordId, "3").WithHomozygousHlaAt(Locus.A, "*01:02").Build(),
                createUpdateBuilder.With(d => d.RecordId, "4").WithHomozygousHlaAt(Locus.B, "*01:02").Build(),
                createUpdateBuilder.With(d => d.RecordId, "5").WithHomozygousHlaAt(Locus.B, "*01:02").Build(),
                createUpdateBuilder.With(d => d.RecordId, "6").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                createUpdateBuilder.With(d => d.RecordId, "7").WithHla(HlaBuilder.New.WithMolecularHlaAtAllLoci("01:01", "01:01").WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:03").WithHomozygousMolecularHlaAtLocus(Locus.C, "*01:04").Build()).Build(),
                createUpdateBuilder.With(d => d.RecordId, "8").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                // recordId "9" hasn't been created yet.
            };

            var createFile = fileBuilder.WithDonors(create)
                .With(f => f.FileLocation, "file1")
                .With(f => f.UploadTime, DateTime.UtcNow.AddDays((-5)))
                .Build();
            await donorFileImporter.ImportDonorFile(createFile);


            //Leave the rows from the previous block in place to help Devs follow the sequence of events.
            var edit = new[]
            {
                //createUpdateBuilder.With(d => d.RecordId, "1").WithHomozygousHlaAt(Locus.A, "*01:01").Build(),
                editUpdateBuilder.With(d => d.RecordId, "2").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                editUpdateBuilder.With(d => d.RecordId, "3").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                //createUpdateBuilder.With(d => d.RecordId, "4").WithHomozygousHlaAt(Locus.B, "*01:02").Build(),
                //createUpdateBuilder.With(d => d.RecordId, "5").WithHomozygousHlaAt(Locus.B, "*01:02").Build(),
                editUpdateBuilder.With(d => d.RecordId, "6").WithHomozygousHlaAt(Locus.B, "*01:04").Build(), //Note this overriding the A property on the original.
                //createUpdateBuilder.With(d => d.RecordId, "7").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:03").WithHomozygousMolecularHlaAtLocus(Locus.C, "*01:04").Build()).Build(),
                //createUpdateBuilder.With(d => d.RecordId, "8").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                // recordId "9" hasn't been created yet.
            };

            var editFile = fileBuilder.WithDonors(edit).With(f => f.FileLocation, "file2")
                .With(f => f.UploadTime, DateTime.Now.AddDays(-4))
                .Build();
            await donorFileImporter.ImportDonorFile(editFile);


            var delete = new[]
            {
                //createUpdateBuilder.With(d => d.RecordId, "1").WithHomozygousHlaAt(Locus.A, "*01:01").Build(),
                deleteUpdateBuilder.With(d => d.RecordId, "2").Build(),
                //editUpdateBuilder.With(d => d.RecordId, "3").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                deleteUpdateBuilder.With(d => d.RecordId, "4").Build(),
                deleteUpdateBuilder.With(d => d.RecordId, "5").Build(),
                //editUpdateBuilder.With(d => d.RecordId, "6").WithHomozygousHlaAt(Locus.B, "*01:04").Build(), //Note this overriding the A property on the original.
                //createUpdateBuilder.With(d => d.RecordId, "7").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:03").WithHomozygousMolecularHlaAtLocus(Locus.C, "*01:04").Build()).Build(),
                //createUpdateBuilder.With(d => d.RecordId, "8").WithHomozygousHlaAt(Locus.A, "*01:03").Build(),
                // recordId "9" hasn't been created yet.
            };

            var deleteFile = fileBuilder.WithDonors(delete)
                .With(f => f.FileLocation, "file3")
                .With(f => f.UploadTime, DateTime.Now.AddDays(-3))
                .Build();
            await donorFileImporter.ImportDonorFile(deleteFile);

            var moreEdit = new[]
            {
                //createUpdateBuilder.With(d => d.RecordId, "1").WithHomozygousHlaAt(Locus.A, "*01:01").Build(),
                //deleteUpdateBuilder.With(d => d.RecordId, "2").Build(),
                editUpdateBuilder.With(d => d.RecordId, "3").WithHomozygousHlaAt(Locus.A, "*01:04").Build(),
                //deleteUpdateBuilder.With(d => d.RecordId, "4").Build(),
                //deleteUpdateBuilder.With(d => d.RecordId, "5").Build(),
                //editUpdateBuilder.With(d => d.RecordId, "6").WithHomozygousHlaAt(Locus.B, "*01:04").Build(), //Note this overriding the A property on the original.
                //createUpdateBuilder.With(d => d.RecordId, "7").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:03").WithHomozygousMolecularHlaAtLocus(Locus.C, "*01:04").Build()).Build(),
                editUpdateBuilder.With(d => d.RecordId, "8").WithHla(HlaBuilder.New.WithMolecularHlaAtAllLoci("01:01", "01:01").WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:06").WithHomozygousMolecularHlaAtLocus(Locus.B, "*01:07").Build()).Build(),
                // recordId "9" hasn't been created yet.
            };

            var moreEditFile = fileBuilder.WithDonors(moreEdit)
                .With(f => f.FileLocation, "file4")
                .With(f => f.UploadTime, DateTime.Now.AddDays(-2))
                .Build();
            await donorFileImporter.ImportDonorFile(moreEditFile);

            var moreCreate = new[]
            {
                //createUpdateBuilder.With(d => d.RecordId, "1").WithHomozygousHlaAt(Locus.A, "*01:01").Build(),
                //deleteUpdateBuilder.With(d => d.RecordId, "2").Build(),
                //editUpdateBuilder.With(d => d.RecordId, "3").WithHomozygousHlaAt(Locus.A, "*01:04").Build(),
                createUpdateBuilder.With(d => d.RecordId, "4").WithHomozygousHlaAt(Locus.C, "*01:11").Build(), //Note this re-creating a previously deleted record.
                //deleteUpdateBuilder.With(d => d.RecordId, "5").Build(),
                //editUpdateBuilder.With(d => d.RecordId, "6").WithHomozygousHlaAt(Locus.B, "*01:04").Build(), //Note this overriding the A property on the original.
                //createUpdateBuilder.With(d => d.RecordId, "7").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:03").WithHomozygousMolecularHlaAtLocus(Locus.C, "*01:04").Build()).Build(),
                //editUpdateBuilder.With(d => d.RecordId, "8").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:06").WithHomozygousMolecularHlaAtLocus(Locus.B, "*01:07").Build()).Build(),
                createUpdateBuilder.With(d => d.RecordId, "9").WithHomozygousHlaAt(Locus.C, "*01:10").Build(),
            };

            var moreCreateFile = fileBuilder.WithDonors(moreCreate)
                .With(f => f.FileLocation, "file5")
                .With(f => f.UploadTime, DateTime.Now.AddDays(-1))
                .Build();
            await donorFileImporter.ImportDonorFile(moreCreateFile);

            const string defaultValidHla = "*01:01";
            var finalDonors = donorRepository.StreamAllDonors().ToList();
            finalDonors.Should().BeEquivalentTo(new[]
                {
                    
                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "1")
                        .With(d => d.A_1,  "*01:01")
                        .With(d => d.A_2,"*01:01")
                        .With(d => d.UpdateFile, "file1")
                        .Build(),

                    // ExternalDonorCode "2" got deleted.
                    
                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "3")
                        .With(d => d.A_1,  "*01:04")
                        .With(d => d.A_2,"*01:04")
                        .With(d => d.UpdateFile, "file4")
                        .Build(),
                    
                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "4")
                        .With(d => d.C_1,  "*01:11")
                        .With(d => d.C_2,"*01:11")
                        .With(d => d.UpdateFile, "file5")
                        .Build(),

                    // ExternalDonorCode "5" got deleted.
                    
                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "6")
                        .With(d => d.B_1,  "*01:04")
                        .With(d => d.B_2,"*01:04")
                        .With(d => d.UpdateFile, "file2")
                        .Build(),

                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "7")
                        .With(d => d.A_1,  "*01:03")
                        .With(d => d.A_2,"*01:03")
                        .With(d => d.C_1,  "*01:04")
                        .With(d => d.C_2,"*01:04")
                        .With(d => d.UpdateFile, "file1")
                        .Build(),
                    
                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "8")
                        .With(d => d.A_1,  "*01:06")
                        .With(d => d.A_2,"*01:06")
                        .With(d => d.B_1,  "*01:07")
                        .With(d => d.B_2,"*01:07")
                        .With(d => d.UpdateFile, "file4")
                        .Build(),
                    
                    DatabaseDonorBuilder.New(defaultValidHla)
                        .With(d => d.ExternalDonorCode, "9")
                        .With(d => d.C_1,  "*01:10")
                        .With(d => d.C_2,"*01:10")
                        .With(d => d.UpdateFile, "file5")
                        .Build(),
                },
                options =>
                    options
                        .Excluding(dbDonor => dbDonor.AtlasId)
                        .Excluding(dbDonor => dbDonor.Hash)
                        .Excluding(dbDonor => dbDonor.LastUpdated)
                        .ExcludingMissingMembers()
           );
        }
        
    }
}