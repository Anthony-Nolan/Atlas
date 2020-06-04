using System;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.DonorTypeParsing
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
        public async Task ImportDonors_WhenUnrecognisedDonorTypeInFile_RejectsFile()
        {
           Assert.ThrowsAsync<JsonSerializationException>(() => ImportDonorFile("invalidDonorType.json"));
           
           var adultDonor = await donorRepository.GetDonor(AdultDonorInInvalidFileId);
           adultDonor.Should().BeNull();
           
           var invalidDonor = await donorRepository.GetDonor(InvalidDonorId);
           invalidDonor.Should().BeNull();
        }
        
        private async Task ImportValidDonorFile()
        {
            await ImportDonorFile("validDonorTypes.json");
        }

        private async Task ImportDonorFile(string fileName)
        {
            
            var donorTestFile = $"Atlas.DonorImport.Test.Integration.IntegrationTests.DonorTypeParsing.{fileName}";
            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFile))
            {
                await donorFileImporter.ImportDonorFile(stream, donorTestFile);
            }
        }
    }
}