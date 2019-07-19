using Nova.DonorService.SearchAlgorithm.Models.DonorInfoForSearchAlgorithm;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
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
        private IDonorService donorService;
        private IDonorManagementService donorManagementService;

        [SetUp]
        public void SetUp()
        {
            donorService = Substitute.For<IDonorService>();

            donorManagementService = new DonorManagementService(donorService);
        }

        [Test]
        public async Task ManageDonorByAvailability_DonorIsAvailableForSearch_DonorIsAddedOrUpdated()
        {
            const int donorId = 456;
            const string registryCode = "AN";
            const string donorType = "A";

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfoForSearchAlgorithm = new DonorInfoForSearchAlgorithm { DonorId = donorId, RegistryCode = registryCode, DonorType = donorType },
                IsAvailableForSearch = true
            });

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<InputDonor>>(x => 
                    x.Single().DonorId == donorId &&
                    x.Single().RegistryCode == RegistryCode.AN &&
                    x.Single().DonorType == DonorType.Adult));
        }

        [Test]
        public async Task ManageDonorByAvailability_DonorIsAvailableForSearch_DonorIsNotDeleted()
        {
            const int donorId = 456;
            const string registryCode = "AN";
            const string donorType = "A";

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfoForSearchAlgorithm = new DonorInfoForSearchAlgorithm { DonorId = donorId, RegistryCode = registryCode, DonorType = donorType },
                IsAvailableForSearch = true
            });

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
        }

        [Test]
        public async Task ManageDonorByAvailability_DonorIsNotAvailableForSearch_DonorIsNotAddedOrUpdated()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false
            });

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }
    }
}
