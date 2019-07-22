using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.Http.Exceptions;
using NSubstitute.ExceptionExtensions;

namespace Nova.SearchAlgorithm.Test.Services
{
    [TestFixture]
    public class DonorManagementServiceTests
    {
        private IDonorService donorService;
        private ILogger logger;
        private IDonorManagementService donorManagementService;

        [SetUp]
        public void SetUp()
        {

            donorService = Substitute.For<IDonorService>();
            logger = Substitute.For<ILogger>();

            donorManagementService = new DonorManagementService(donorService, logger);
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
                DonorInfo = new DonorInfo { DonorId = donorId, RegistryCode = registryCode, DonorType = donorType },
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
                DonorInfo = new DonorInfo { DonorId = donorId, RegistryCode = registryCode, DonorType = donorType },
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

        [Test]
        public async Task ManageDonorByAvailability_DonorForDeletionNotFound_ErrorIsLogged()
        {
            const int donorId = 789;

            donorService.DeleteDonor(donorId).Throws(new NovaNotFoundException("error-message"));

            await donorManagementService.ManageDonorByAvailability(new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false
            });

            logger.Received().SendEvent(Arg.Is<DonorDeletionFailureEventModel>(x => 
                x.Level == LogLevel.Error &&
                x.Properties.ContainsKey("DonorId") &&
                x.Properties.ContainsValue(donorId.ToString())));
        }
    }
}
