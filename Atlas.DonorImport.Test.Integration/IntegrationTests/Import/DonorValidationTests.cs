using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Models.FileSchema;
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
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task ImportDonors_IfMissingMandatoryHlaTypings_DoesNotAddToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();

            donorUpdate.Hla.A = null;
            var file = fileBuilder.WithDonors(donorUpdate).Build();

            await donorFileImporter.ImportDonorFile(file);

            var result = await donorRepository.GetDonor(donorUpdate.RecordId);
            result.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_IfRequiredLocusHasNullHlaValues_DoesNotAddToDatabase()
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
        public async Task ImportDonors_IfRequiredLocusHasEmptyValues_DoesNotAddToDatabase()
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
        public async Task ImportDonors_IfRequiredLocusIsMissingFirstPositionHla_DoesNotAddToDatabase()
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
        public async Task ImportDonors_IfRequiredLocusIsMissingSecondPositionHla_AddsToDatabaseAsHomozygous()
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

        [Test]
        public async Task ImportDonors_WhenOptionalHlaNotIncluded_AddsToDatabase()
        {
            var donorUpdate = DonorCreationBuilder.Build();
            donorUpdate.Hla.DQB1 = new ImportedLocus();

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
    }
}