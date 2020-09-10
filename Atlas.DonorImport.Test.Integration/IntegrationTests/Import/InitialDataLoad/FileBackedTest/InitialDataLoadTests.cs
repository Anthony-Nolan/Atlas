using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Utils.Extensions;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.Import.InitialDataLoad.FileBackedTest
{
    /// <summary>
    /// While most of the other tests in this suite can use memory streams of files built by the test themselves, this test runs on a real input file.
    /// This grants us extra certainty that there are no issues in our serialisation of in-memory files in other tests.
    /// </summary>
    [TestFixture]
    public class InitialDataLoadTests
    {
        private IDonorInspectionRepository donorRepository;

        private IDonorFileImporter donorFileImporter;
        private IMessagingServiceBusClient mockServiceBusClient;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () =>
            {
                mockServiceBusClient = Substitute.For<IMessagingServiceBusClient>();
                var services = DependencyInjection.ServiceConfiguration.BuildServiceCollection();
                services.AddScoped(sp => mockServiceBusClient);
                DependencyInjection.DependencyInjection.BackingProvider = services.BuildServiceProvider();
                
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                // Run operation under test once for this fixture, to (a) improve performance (b) remove the need to clean up duplicate ids between runs
                await ImportFile();
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

        [Test]
        public void ImportDonors_AllNewDonors_AreAddedCorrectly()
        {
            const string expectedDonorHash = "jwDboXz3AUJrkMMi/MZhVA==";

            var actualDonors = donorRepository.StreamAllDonors().ToList();

            var actualCombinedStoredHash = actualDonors.Select(donor => donor.Hash).StringJoin("#").ToMd5Hash();
            actualCombinedStoredHash.Should().Be(expectedDonorHash);

            var actualCombinedCalculatedHash = actualDonors.ToList().Select(donor => donor.CalculateHash()).StringJoin("#").ToMd5Hash();
            actualCombinedCalculatedHash.Should().Be(expectedDonorHash);
        }

        [Test]
        public void ImportDonors_DoesNotSendNotificationsForMatchingAlgorithm()
        {
            mockServiceBusClient.DidNotReceiveWithAnyArgs().PublishDonorUpdateMessages(default);
        }

        private async Task ImportFile()
        {
            const string fileName = "1000-initial-donors.json";
            // Relies on namespace matching file nesting, but is resilient to file re-structure.
            var donorTestFilePath = $"{typeof(InitialDataLoadTests).Namespace}.{fileName}";
            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFilePath))
            {
                await donorFileImporter.ImportDonorFile(
                    new DonorImportFile{ Contents = stream, FileLocation = fileName, UploadTime = DateTime.Now, MessageId = "message-id"});
            }
        }
    }
}