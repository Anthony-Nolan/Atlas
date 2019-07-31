using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Services.Donors;
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
        public async Task ManageDonorBatchByAvailability_DonorIsAvailableForSearch_DonorIsAddedOrUpdated()
        {
            const int donorId = 456;
            const RegistryCode registryCode = RegistryCode.AN;
            const DonorType donorType = DonorType.Adult;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new InputDonor { DonorId = donorId, RegistryCode = registryCode, DonorType = donorType },
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<InputDonor>>(x =>
                    x.Single().DonorId == donorId &&
                    x.Single().RegistryCode == registryCode &&
                    x.Single().DonorType == donorType));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsAvailableForSearch_DonorIsNotDeleted()
        {
            const int donorId = 456;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new InputDonor { DonorId = donorId, RegistryCode = RegistryCode.AN, DonorType = DonorType.Adult },
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(0)
                .DeleteDonorBatch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsNotAvailableForSearch_DonorIsRemoved()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(1)
                .DeleteDonorBatch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsNotAvailableForSearch_DonorIsNotAddedOrUpdated()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }
    }
}
