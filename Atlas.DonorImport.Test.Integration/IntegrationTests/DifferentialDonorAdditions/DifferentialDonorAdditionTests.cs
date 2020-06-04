using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Integration.IntegrationTests.DifferentialDonorAdditions
{
    [TestFixture]
    public class DifferentialDonorAdditionTests
    {
        private IDonorInspectionRepository donorRepository;
        private IMessagingServiceBusClient serviceBusClient;

        private IDonorFileImporter donorFileImporter;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                serviceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<IMessagingServiceBusClient>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
                // Run operation under test once for this fixture, to (a) improve performance (b) remove the need to clean up duplicate ids between tests within this fixture
                await ImportFile();
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        public async Task ImportDonors_ForAllAdditions_SendsMatchingUpdate()
        {
            await serviceBusClient.Received(2).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }

        [Test]
        public async Task ImportDonors_SendsMatchingUpdateWithNewlyAssignedAtlasId()
        {
            var donor1 = await donorRepository.GetDonor("external-donor-code-1");
            var donor2 = await donorRepository.GetDonor("external-donor-code-2");

            donor1.AtlasId.Should().NotBe(donor2.AtlasId);
            await serviceBusClient.Received().PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == donor1.AtlasId && u.SearchableDonorInformation.DonorId == donor1.AtlasId)
            );
            await serviceBusClient.Received().PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == donor2.AtlasId && u.SearchableDonorInformation.DonorId == donor2.AtlasId)
            );
        }

        private async Task ImportFile()
        {
            const string donorTestFile = "Atlas.DonorImport.Test.Integration.IntegrationTests.DifferentialDonorAdditions.test-data.json";
            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(donorTestFile))
            {
                await donorFileImporter.ImportDonorFile(stream, donorTestFile);
            }
        }
    }
}