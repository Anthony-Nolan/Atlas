using Nova.DonorService.SearchAlgorithm.Models.DonorInfoForSearchAlgorithm;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Clients;
using Nova.SearchAlgorithm.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services
{
    [TestFixture]
    public class DonorManagementServiceTests
    {
        private IDonorServiceClient donorServiceClient;
        private IDonorService donorService;
        private IDonorManagementService donorManagementService;

        [SetUp]
        public void SetUp()
        {
            donorServiceClient = Substitute.For<IDonorServiceClient>();
            donorService = Substitute.For<IDonorService>();

            donorServiceClient
                .GetDonorInfoForSearchAlgorithm(Arg.Any<int>())
                .Returns(new DonorInfoForSearchAlgorithm
                {
                    DonorId = 999999,
                    DonorType = "Adult",
                    RegistryCode = "AN"
                });

            donorManagementService = new DonorManagementService(donorServiceClient, donorService);
        }

        [Test]
        public async Task ManageDonorByAvailability_DonorIsAvailableForSearch_GetsDonorInfo()
        {
            const int donorId = 123;

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = true
            });

            await donorServiceClient
                .Received(1)
                .GetDonorInfoForSearchAlgorithm(donorId);
        }

        [Test]
        public async Task ManageDonorByAvailability_DonorIsAvailableForSearch_DonorIsAdded()
        {
            const int donorId = 456;

            donorServiceClient
                .GetDonorInfoForSearchAlgorithm(donorId)
                .Returns(new DonorInfoForSearchAlgorithm
                {
                    DonorId = donorId,
                    DonorType = "Adult",
                    RegistryCode = "AN"
                });

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = true
            });

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<InputDonor>>(x => x.Single().DonorId == donorId));

            await donorService
                .Received(0)
                .DeleteDonor(Arg.Any<int>());
        }

        [Test]
        public async Task ManageDonorByAvailability_DonorIsNotAvailableForSearch_DonorIsRemoved()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false
            });

            await donorService
                .Received(1)
                .DeleteDonor(Arg.Is<int>(x => x == donorId));

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }
    }
}
