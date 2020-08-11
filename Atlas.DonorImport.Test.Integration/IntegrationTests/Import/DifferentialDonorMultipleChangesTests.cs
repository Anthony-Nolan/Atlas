using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Donor = Atlas.DonorImport.Data.Models.Donor;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    /* ===============================================================
     * ===============================================================
     * ====                README                                 ====
     * ====     This tests are a small snapshot test.             ====
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
                createUpdateBuilder.With(d => d.RecordId, "7").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:03").WithHomozygousMolecularHlaAtLocus(Locus.C, "*01:04").Build()).Build(),
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
                editUpdateBuilder.With(d => d.RecordId, "8").WithHla(HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(Locus.A, "*01:06").WithHomozygousMolecularHlaAtLocus(Locus.B, "*01:07").Build()).Build(),
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


            var finalDonors = donorRepository.StreamAllDonors().ToList();
            finalDonors.Should().BeEquivalentTo(new[]
                {
                    new Donor
                    {
                        ExternalDonorCode = "1",
                        AtlasId = 1,
                        A_1 = "*01:01",
                        A_2 = "*01:01",
                        UpdateFile = "file1"
                    },

                    // ExternalDonorCode "2" got deleted.
                    
                    new Donor
                    {
                        ExternalDonorCode = "3",
                        AtlasId = -1,
                        A_1 = "*01:04",
                        A_2 = "*01:04",
                        UpdateFile = "file4"
                    },
                    new Donor
                    {
                        ExternalDonorCode = "4",
                        AtlasId = -1,
                        C_1 = "*01:11",
                        C_2 = "*01:11",
                        UpdateFile = "file5"
                    },

                    // ExternalDonorCode "5" got deleted.
                    
                    new Donor
                    {
                        ExternalDonorCode = "6",
                        AtlasId = -1,
                        B_1 = "*01:04",
                        B_2 = "*01:04",
                        UpdateFile = "file2"
                    },
                    new Donor
                    {
                        ExternalDonorCode = "7",
                        AtlasId = -1,
                        A_1 = "*01:03",
                        A_2 = "*01:03",
                        C_1 = "*01:04",
                        C_2 = "*01:04",
                        UpdateFile = "file1"
                    },
                    new Donor
                    {
                        ExternalDonorCode = "8",
                        AtlasId = -1,
                        A_1 = "*01:06",
                        A_2 = "*01:06",
                        B_1 = "*01:07",
                        B_2 = "*01:07",
                        UpdateFile = "file4"
                    },
                    new Donor
                    {
                        ExternalDonorCode = "9",
                        AtlasId = -1,
                        C_1 = "*01:10",
                        C_2 = "*01:10",
                        UpdateFile = "file5"
                    },
                },
                options =>
                    options
                        .Excluding(dbDonor => dbDonor.AtlasId)
                        .Excluding(dbDonor => dbDonor.Hash)
                        .Excluding(dbDonor => dbDonor.LastUpdated)
           );
        }
    }
}