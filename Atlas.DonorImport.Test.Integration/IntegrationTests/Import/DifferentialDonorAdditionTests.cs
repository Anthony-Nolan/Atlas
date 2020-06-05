using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Clients;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.Integration.TestHelpers;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
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
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorRepository = DependencyInjection.DependencyInjection.Provider.GetService<IDonorInspectionRepository>();
                serviceBusClient = DependencyInjection.DependencyInjection.Provider.GetService<IMessagingServiceBusClient>();
                donorFileImporter = DependencyInjection.DependencyInjection.Provider.GetService<IDonorFileImporter>();
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
            const int numberOfDonors = 5;
            var donorUpdateFile = DonorImportFileBuilder.WithDefaultMetaData.WithDonors(numberOfDonors).Build();

            await donorFileImporter.ImportDonorFile(donorUpdateFile);
            
            await serviceBusClient.Received(numberOfDonors).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }

        [Test]
        public async Task ImportDonors_SendsMatchingUpdateWithNewlyAssignedAtlasId()
        {
            const string donorCodePrefix = "external-donor-code";
            var donorUpdate1 = DonorUpdateBuilder.ForRecordId(IncrementingIdGenerator.NextStringId(donorCodePrefix)).Build();
            var donorUpdate2 = DonorUpdateBuilder.ForRecordId(IncrementingIdGenerator.NextStringId(donorCodePrefix)).Build();
            var donorUpdateFile = DonorImportFileBuilder.WithDefaultMetaData.WithDonors(donorUpdate1, donorUpdate2);
            
            await donorFileImporter.ImportDonorFile(donorUpdateFile);
            
            var donor1 = await donorRepository.GetDonor(donorUpdate1.RecordId);
            var donor2 = await donorRepository.GetDonor(donorUpdate2.RecordId);
            
            donor1.AtlasId.Should().NotBe(donor2.AtlasId);
            await serviceBusClient.Received().PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == donor1.AtlasId && u.SearchableDonorInformation.DonorId == donor1.AtlasId)
            );
            await serviceBusClient.Received().PublishDonorUpdateMessage(Arg.Is<SearchableDonorUpdate>(u =>
                u.DonorId == donor2.AtlasId && u.SearchableDonorInformation.DonorId == donor2.AtlasId)
            );
        }
    }
}