using System.Reflection;
using System.Threading.Tasks;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.InitialDataLoad
{
    [TestFixture]
    public class InitialDataLoadTests
    {
        private IDonorInspectionRepository donorRepository;
        
        private IDonorFileImporter donorFileImporter;
        
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
            donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
            // Run operation under test once for this fixture, to (a) improve performance (b) remove the need to clean up duplicate ids between runs
            await ImportFile();
        }

        [Test]
        public async Task ImportDonors_ForAllAdditions_AddsAllDonorsToDatabase()
        {
            var importedDonors = await donorRepository.DonorCount();

            importedDonors.Should().Be(1000);
        }

        /// <summary>
        /// Snapshot test of an arbitrary donor, to test mapping and plumbing working as expected
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task ImportDonors_NewDonor_IsAddedCorrectly()
        {
            const string selectedDonorId = "1";
            const string expectedDonorHash = "MHH/OTtSeI96PClybhTF0g=="; 

            var actualDonor = await donorRepository.GetDonor(selectedDonorId);
            
            actualDonor.Hash.Should().Be(expectedDonorHash);
            actualDonor.CalculateHash().Should().Be(expectedDonorHash);
        }

        private async Task ImportFile()
        {
            const string donorTestFile = "Atlas.DonorImport.Test.Integration.IntegrationTests.InitialDataLoad.1000-initial-donors.json";
            await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFile);
            await donorFileImporter.ImportDonorFile(stream, donorTestFile);
        }
    }
}