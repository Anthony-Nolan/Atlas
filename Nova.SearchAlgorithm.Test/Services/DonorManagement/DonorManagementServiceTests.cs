using AutoMapper;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.Donors;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class DonorManagementServiceTests
    {
        private IDonorManagementLogRepository logRepository;
        private IDonorService donorService;
        private IDonorManagementService donorManagementService;
        private IMapper mapper;

        [SetUp]
        public void SetUp()
        {
            logRepository = Substitute.For<IDonorManagementLogRepository>();
            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new DonorManagementLog[] { });

            var repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            repositoryFactory.GetDonorManagementLogRepository().Returns(logRepository);

            donorService = Substitute.For<IDonorService>();
            var logger = Substitute.For<ILogger>();
            mapper = Substitute.For<IMapper>();

            donorManagementService = new DonorManagementService(repositoryFactory, donorService, logger, mapper);
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsAvailableForSearch_AddsOrUpdatesDonor()
        {
            const int donorId = 456;
            const RegistryCode registryCode = RegistryCode.AN;
            const DonorType donorType = DonorType.Adult;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = 1,
                    DonorInfo = new InputDonor
                    {
                        DonorId = donorId,
                        RegistryCode = registryCode,
                        DonorType = donorType
                    },
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
        public async Task ManageDonorBatchByAvailability_DonorIsAvailableForSearch_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 456;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = 1,
                    DonorInfo = new InputDonor
                    {
                        DonorId = donorId,
                        RegistryCode = RegistryCode.AN,
                        DonorType = DonorType.Adult
                    },
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(0)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsNotAvailableForSearch_SetsDonorAsUnavailable()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = 1,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsNotAvailableForSearch_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = 1,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsAvailableInLatest_AddsOrUpdatesDonor()
        {
            const int donorId = 456;
            const RegistryCode registryCode = RegistryCode.AN;
            const DonorType donorType = DonorType.Adult;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 1,
                    DonorId = donorId,
                    IsAvailableForSearch = false
                },
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 100,
                    DonorId = donorId,
                    DonorInfo = new InputDonor
                    {
                        DonorId = donorId,
                        RegistryCode = registryCode,
                        DonorType = donorType
                    },
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
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsAvailableInLatest_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 456;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 1,
                    DonorId = donorId,
                    IsAvailableForSearch = false
                },
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 100,
                    DonorId = donorId,
                    DonorInfo = new InputDonor
                    {
                        DonorId = donorId,
                        RegistryCode = RegistryCode.AN,
                        DonorType = DonorType.Adult
                    },
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(0)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsUnavailableInLatest_SetsDonorAsUnavailable()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 1,
                    DonorId = donorId,
                    IsAvailableForSearch = true
                },
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 100,
                    DonorId = donorId,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsUnavailableInLatest_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 1,
                    DonorId = donorId,
                    IsAvailableForSearch = true
                },
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 100,
                    DonorId = donorId,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdatesContainAvailableAndUnavailableDonors_ModifiesDonorsCorrectly()
        {
            const int availableDonorId = 123;
            const int unavailableDonorId = 456;
            const RegistryCode registryCode = RegistryCode.AN;
            const DonorType donorType = DonorType.Adult;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 1,
                    DonorId = unavailableDonorId,
                    IsAvailableForSearch = false
                },
                new DonorAvailabilityUpdate
                {
                    UpdateSequenceNumber = 2,
                    DonorId = availableDonorId,
                    DonorInfo = new InputDonor
                    {
                        DonorId = availableDonorId,
                        RegistryCode = registryCode,
                        DonorType = donorType
                    },
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == unavailableDonorId));

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<InputDonor>>(x =>
                    x.Single().DonorId == availableDonorId &&
                    x.Single().RegistryCode == registryCode &&
                    x.Single().DonorType == donorType));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_CreatesOrUpdatesDonorManagementLog()
        {
            const int donorId = 789;
            const int sequenceNumber = 123456789;

            mapper.Map<IEnumerable<DonorManagementInfo>>(
                    Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(x => x.Single().DonorId == donorId))
                .Returns(new List<DonorManagementInfo>
                {
                    new DonorManagementInfo
                    {
                        DonorId = donorId,
                        UpdateSequenceNumber = sequenceNumber
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumber
                }});

            await logRepository
                .Received(1)
                .CreateOrUpdateDonorManagementLogBatch(Arg.Is<IEnumerable<DonorManagementInfo>>(x =>
                    x.Single().DonorId == donorId &&
                    x.Single().UpdateSequenceNumber == sequenceNumber));
        }
    }
}
