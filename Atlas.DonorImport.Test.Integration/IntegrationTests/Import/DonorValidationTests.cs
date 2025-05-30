using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import
{
    [TestFixture]
    public class DonorValidationTests
    {
        private IDonorInspectionRepository donorRepository;
        private IDonorFileImporter donorFileImporter;
        private IDonorImportFailuresInspectionRepository donorImportFailureInspectionRepository;
        private IDonorImportFailuresCleaner donorImportFailuresCleaner;
        private FailureLogsSettings failureLogsSettings;

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
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                donorImportFailureInspectionRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImportFailuresInspectionRepository>();
                donorImportFailuresCleaner = DependencyInjection.DependencyInjection.Provider.GetService<IDonorImportFailuresCleaner>();
                failureLogsSettings = DependencyInjection.DependencyInjection.Provider.GetService<FailureLogsSettings>();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        #region RequiredLoci

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.Drb1)]
        public async Task ImportDonors_WhenRequiredLocusMissing_DoesNotAddToDatabase(Locus locus)
        {
            var donorUpdate = DonorCreationBuilder.Build();

            switch (locus)
            {
                case Locus.A:
                    donorUpdate.Hla.A = null;
                    break;
                case Locus.B:
                    donorUpdate.Hla.B = null;
                    break;
                case Locus.Drb1:
                    donorUpdate.Hla.DRB1 = null;
                    break;
            }

            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_ForDonorEdit_WhenRequiredLocusMissing_DoesNotChangeDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();
            var file = fileBuilder.WithDonors(donorUpdate).Build();
            await donorFileImporter.ImportDonorFile(file);

            var modifiedDonor = DonorUpdateBuilder.New
                    .With(du => du.RecordId, donorUpdate.RecordId)
                    .With(du => du.Hla, HlaBuilder.Default.WithMolecularHlaAtLocus(Locus.A, null, null))
                    .With(upd => upd.ChangeType, ImportDonorChangeType.Edit)
                    .Build();
            var modifiedDonorFile = fileBuilder.WithDonors(modifiedDonor).Build();
            
            await donorFileImporter.ImportDonorFile(modifiedDonorFile);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
            result.A_1.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task ImportDonors_ForDonorUpsert_WhenDonorNotExistsAndRequiredLocusMissing_DoesNotChangeDatabase()
        {
            var donorUpdate = DonorUpdateBuilder.New
                .WithRecordIdPrefix("external-donor-code-")
                .With(du => du.Hla, HlaBuilder.Default.WithMolecularHlaAtLocus(Locus.A, null, null))
                .With(upd => upd.ChangeType, ImportDonorChangeType.Upsert)
                .Build();
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);
            
            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_ForDonorUpsert_WhenDonorExistsAndRequiredLocusMissing_DoesNotChangeDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();
            var file = fileBuilder.WithDonors(donorUpdate).Build();
            await donorFileImporter.ImportDonorFile(file);

            var modifiedDonor = DonorUpdateBuilder.New
                .With(du => du.RecordId, donorUpdate.RecordId)
                .With(du => du.Hla, HlaBuilder.Default.WithMolecularHlaAtLocus(Locus.A, null, null))
                .With(upd => upd.ChangeType, ImportDonorChangeType.Upsert)
                .Build();
            var modifiedDonorFile = fileBuilder.WithDonors(modifiedDonor).Build();

            await donorFileImporter.ImportDonorFile(modifiedDonorFile);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
            result.A_1.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task ImportDonors_WhenRequiredLocusHasNullHlaValues_DoesNotAddToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

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
        public async Task ImportDonors_WhenRequiredLocusHasEmptyValues_DoesNotAddToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

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
        public async Task ImportDonors_WhenRequiredLocusIsMissingFirstPositionHla_DoesNotAddToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.A = new ImportedLocus
            {
                Dna = new TwoFieldStringData
                {
                    Field1 = "",
                    Field2 = "*01:01"
                }
            };
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_WhenRequiredLocusIsMissingSecondPositionHla_AddsToDatabaseAsHomozygous()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            const string hla = "*01:01";
            donorUpdate.Hla.A = new ImportedLocus
            {
                Dna = new TwoFieldStringData
                {
                    Field1 = hla,
                    Field2 = ""
                }
            };
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
            result.A_1.Should().Be(hla);
            result.A_2.Should().Be(hla);
        }

        [Test]
        public async Task ImportDonors_WhenRequiredLocusHasMolecularValuesOnly_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.A = new ImportedLocus {Dna = new TwoFieldStringData {Field1 = "01:01", Field2 = "01:01"}};
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenRequiredLocusHasSerologyValuesOnly_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.A = new ImportedLocus {Serology = new TwoFieldStringData {Field1 = "11", Field2 = "11"}};
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenRequiredLocusHasNeitherMolecularNorSerologyValues_DoesNotAddToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.A = new ImportedLocus();
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_WhenRequiredLocusHasMolecularAndSerologyValues_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.A = new ImportedLocus
            {
                Dna = new TwoFieldStringData {Field1 = "01:01", Field2 = "01:01"},
                Serology = new TwoFieldStringData {Field1 = "11", Field2 = "11"}
            };
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        #endregion

        [Test]
        public async Task ImportDonors_ForDonorDeletion_WithInvalidHla_DonorIsStillDeleted()
        {
            var donorUpdate = DonorCreationBuilder.Build();
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

        #region OptionalLoci

        [TestCase(Locus.C)]
        [TestCase(Locus.Dpb1)]
        [TestCase(Locus.Dqb1)]
        public async Task ImportDonors_WhenOptionalHlaNotIncluded_AddsToDatabase(Locus locus)
        {
            var donorUpdate = DonorCreationBuilder.Build();

            switch (locus)
            {
                case Locus.C:
                    donorUpdate.Hla.C = null;
                    break;
                case Locus.Dpb1:
                    donorUpdate.Hla.DPB1 = null;
                    break;
                case Locus.Dqb1:
                    donorUpdate.Hla.DQB1 = null;
                    break;
            }

            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenOptionalHlaIncludedButEmpty_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();
            donorUpdate.Hla.DQB1 = new ImportedLocus {Dna = new TwoFieldStringData {Field1 = "", Field2 = ""}};

            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenOptionalHlaIncluded_AndOnlyPosition1IsPresent_AddsToDatabaseAsHomozygous()
        {
            var donorUpdate = DonorCreationBuilder.Build();
            const string hla = "*01:01";
            donorUpdate.Hla.DQB1 = new ImportedLocus {Dna = new TwoFieldStringData {Field1 = hla, Field2 = ""}};

            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
            result.DQB1_1.Should().Be(hla);
            result.DQB1_2.Should().Be(hla);
        }

        [Test]
        public async Task ImportDonors_WhenOptionalHlaIncluded_AndOnlyPosition2IsPresent_DoesNotAddToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();
            donorUpdate.Hla.DQB1 = new ImportedLocus {Dna = new TwoFieldStringData {Field1 = "", Field2 = "01:01"}};

            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_WhenOptionalLocusHasMolecularValuesOnly_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.C = new ImportedLocus {Dna = new TwoFieldStringData {Field1 = "01:01", Field2 = "01:01"}};
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenOptionalLocusHasSerologyValuesOnly_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.C = new ImportedLocus {Serology = new TwoFieldStringData {Field1 = "11", Field2 = "11"}};
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenOptionalLocusHasNeitherMolecularNorSerologyValues_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.C = new ImportedLocus();
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        [Test]
        public async Task ImportDonors_WhenOptionalLocusHasMolecularAndSerologyValues_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.C = new ImportedLocus
            {
                Dna = new TwoFieldStringData {Field1 = "01:01", Field2 = "01:01"},
                Serology = new TwoFieldStringData {Field1 = "11", Field2 = "11"}
            };
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().NotBeNull();
        }

        #endregion

        [Test]
        public async Task ImportDonors_WhenDonorUpdateIsInvalid_ValidationErrorsSavedToTable()
        {
            var fileName = Guid.NewGuid().ToString();
            var donorUpdate = DonorCreationBuilder.Build();
            donorUpdate.Hla.DQB1 = new ImportedLocus { Dna = new TwoFieldStringData { Field1 = "", Field2 = "01:01" } };

            var file = fileBuilder.WithDonors(donorUpdate).With(x => x.FileLocation, fileName).Build();

            await donorFileImporter.ImportDonorFile(file);

            var validationErrors = (await donorImportFailureInspectionRepository.GetFailuresByFilename(fileName)).ToList();
            validationErrors.Should().HaveCount(1);
            var error = validationErrors.Single();
            error.FailureReason.Should().Be("Optional locus Dqb1, Dna: Field1 cannot be empty when Field2 is provided");
            error.ExternalDonorCode.Should().Be(donorUpdate.RecordId);
        }

        [Test]
        public async Task ImportDonors_WhenSeveralDonorUpdatesAreInvalid_AllValidationErrorsSavedToTable()
        {
            var fileName = Guid.NewGuid().ToString();
            (var donorUpdate1, var donorUpdate2) = (DonorCreationBuilder.Build(), DonorCreationBuilder.Build());
            donorUpdate1.Hla.DQB1 = new ImportedLocus { Dna = new TwoFieldStringData { Field1 = "", Field2 = "01:01" } };
            donorUpdate2.Hla.A = null;

            var file = fileBuilder.WithDonors(donorUpdate1, donorUpdate2).With(x => x.FileLocation, fileName).Build();

            await donorFileImporter.ImportDonorFile(file);

            var validationErrors = (await donorImportFailureInspectionRepository.GetFailuresByFilename(fileName)).ToList();
            validationErrors.Should().HaveCount(2);
            var validationError1 = validationErrors.Single(x => x.ExternalDonorCode == donorUpdate1.RecordId);
            var validationError2 = validationErrors.Single(x => x.ExternalDonorCode == donorUpdate2.RecordId);

            validationError1.FailureReason.Should().NotBe(validationError2.FailureReason);
        }

        [Test]
        public async Task ImportDonors_WhenDonorUpdateIsValid_NoValidationErrorsSavedToTable()
        {
            var fileName = Guid.NewGuid().ToString();
            var donorUpdate = DonorCreationBuilder.Build();

            const string hla = "*01:01";
            donorUpdate.Hla.A = new ImportedLocus
            {
                Dna = new TwoFieldStringData
                {
                    Field1 = hla,
                    Field2 = ""
                }
            };
            var file = fileBuilder.WithDonors(donorUpdate).With(x => x.FileLocation, fileName).Build();

            await donorFileImporter.ImportDonorFile(file);

            var validationErrors = (await donorImportFailureInspectionRepository.GetFailuresByFilename(fileName)).ToList();
            validationErrors.Should().BeEmpty();
        }

        [Test]
        public async Task DeleteExpiredDonorImportFailures_ExpiredFailuresRemovedFromDB()
        {
            // Adding expired failures
            await donorImportFailureInspectionRepository.CreateRecords(5, DateTime.Now.AddDays(-failureLogsSettings.ExpiryInDays - 1));

            // Adding non expired failures
            var fileName = Guid.NewGuid().ToString();
            var donorUpdate = DonorCreationBuilder.Build();
            donorUpdate.Hla.DQB1 = new ImportedLocus { Dna = new TwoFieldStringData { Field1 = "", Field2 = "01:01" } };

            var file = fileBuilder.WithDonors(donorUpdate).With(x => x.FileLocation, fileName).Build();
            await donorFileImporter.ImportDonorFile(file);

            //Act
            await donorImportFailuresCleaner.DeleteExpiredDonorImportFailures();

            var failures = await donorImportFailureInspectionRepository.GetAll();
            var cutOffDate = DateTime.Now - TimeSpan.FromDays(failureLogsSettings.ExpiryInDays);

            foreach (var f in failures)
            {
                f.FailureTime.Should().BeAfter(cutOffDate);
            }
        }

    }
}