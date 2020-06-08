using System.Linq;
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
            var donorUpdateFile = DonorImportFileBuilder.NewWithoutContents.WithDonorCount(numberOfDonors).Build();

            await donorFileImporter.ImportDonorFile(donorUpdateFile);
            
            await serviceBusClient.Received(numberOfDonors).PublishDonorUpdateMessage(Arg.Any<SearchableDonorUpdate>());
        }

        [Test]
        public async Task ImportDonors_SendsMatchingUpdateWithNewlyAssignedAtlasId()
        {
            const string donorCodePrefix = "external-donor-code";
            var donorUpdates = DonorUpdateBuilder.New.WithRecordIdPrefix(donorCodePrefix).Build(2).ToArray();
            var donorUpdateFile = DonorImportFileBuilder.NewWithoutContents.WithDonors(donorUpdates);
            
            await donorFileImporter.ImportDonorFile(donorUpdateFile);
            
            var donor1 = await donorRepository.GetDonor(donorUpdates[0].RecordId);
            var donor2 = await donorRepository.GetDonor(donorUpdates[1].RecordId);
            
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