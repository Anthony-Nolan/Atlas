using System;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.DonorTypeParsing
{
    [TestFixture]
    public class DonorTypeParsingTests
    {
        private IDonorInspectionRepository donorRepository;

        private IDonorFileImporter donorFileImporter;

        // These are set in the corresponding resource files - ids must be updated in both places
        private const string AdultDonorId = "1";
        private const string CordDonorId = "2";

        private const string AdultDonorInBankedFileId = "3";
        private const string BankedDonorId = "4";

        private const string AdultDonorInInvalidFileId = "5";
        private const string InvalidDonorId = "6";

        private const string SerologyTypedDonorId = "7";

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                // Run operation under test once for this fixture, to (a) improve performance (b) remove the need to clean up duplicate ids between runs
                await ImportValidDonorFile();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task ImportDonors_ParsesAdultDonorTypeCorrectly()
        {
            var actualDonor = await donorRepository.GetDonor(AdultDonorId);

            actualDonor.DonorType.Should().Be(DatabaseDonorType.Adult);
        }

        [Test]
        public async Task ImportDonors_ParsesCordDonorTypeCorrectly()
        {
            var actualDonor = await donorRepository.GetDonor(CordDonorId);

            actualDonor.DonorType.Should().Be(DatabaseDonorType.Cord);
        }

        [Test]
        public async Task ImportDonors_WhenBankedDonorTypeInFile_RejectsFile()
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => ImportDonorFile("bankedDonor.json"));

            var adultDonor = await donorRepository.GetDonor(AdultDonorInBankedFileId);
            adultDonor.Should().BeNull();

            var bankedDonor = await donorRepository.GetDonor(BankedDonorId);
            bankedDonor.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_WhenUnrecognisedDonorTypeInFile_DoesNotImportDonor()
        {
            await ImportDonorFile("invalidDonorType.json");

            var adultDonor = await donorRepository.GetDonor(AdultDonorInInvalidFileId);
            adultDonor.Should().BeNull();

            var invalidDonor = await donorRepository.GetDonor(InvalidDonorId);
            invalidDonor.Should().BeNull();
        }

        [Test]
        public async Task ImportDonors_WhenDonorSerologyTypedOnly_WillImportDonor()
        {
            await ImportDonorFile("serologyTypedDonor.json");

            var donor = await donorRepository.GetDonor(SerologyTypedDonorId);
            donor.ExternalDonorCode.Should().Be(SerologyTypedDonorId);
        }

        private async Task ImportValidDonorFile()
        {
            await ImportDonorFile("validDonorTypes.json");
        }

        private async Task ImportDonorFile(string fileName)
        {
            // Relies on namespace matching file nesting, but is resilient to file re-structure.
            var donorTestFilePath = $"{typeof(DonorTypeParsingTests).Namespace}.{fileName}";
            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFilePath))
            {
                await donorFileImporter.ImportDonorFile(
                    new DonorImportFile {Contents = stream, FileLocation = donorTestFilePath, UploadTime = DateTime.Now, MessageId = "message-id"});
            }
        }
    }
}