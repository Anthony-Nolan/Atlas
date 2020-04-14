using AutoMapper;
using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class DonorManagementServiceTests
    {
        private IDonorManagementLogRepository logRepository;
        private IDonorService donorService;
        private IDonorManagementService donorManagementService;
        private ILogger logger;
        private IMapper mapper;

        [SetUp]
        public void SetUp()
        {
            logRepository = Substitute.For<IDonorManagementLogRepository>();
            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new DonorManagementLog[] { });

            var repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            repositoryFactory.GetDonorManagementLogRepository().Returns(logRepository);

            donorService = Substitute.For<IDonorService>();
            logger = Substitute.For<ILogger>();
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
                    DonorInfo = new DonorInfo
                    {
                        DonorId = donorId,
                        RegistryCode = registryCode,
                        DonorType = donorType
                    },
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<DonorInfo>>(x =>
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
                    DonorInfo = new DonorInfo
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
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsAvailableInLatest_AddsOrUpdatesDonor()
        {
            const int donorId = 456;
            const RegistryCode registryCode = RegistryCode.AN;
            const DonorType donorType = DonorType.Adult;

            var olderUnavailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100))
            };

            var newerAvailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfo = new DonorInfo
                {
                    DonorId = donorId,
                    RegistryCode = registryCode,
                    DonorType = donorType
                },
                IsAvailableForSearch = true,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1))
            };

            await donorManagementService.ManageDonorBatchByAvailability(
                new[] { olderUnavailableUpdate, newerAvailableUpdate });

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<DonorInfo>>(x =>
                    x.Single().DonorId == donorId &&
                    x.Single().RegistryCode == registryCode &&
                    x.Single().DonorType == donorType));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsAvailableInLatest_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 456;

            var olderUnavailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100))
            };

            var newerAvailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfo = new DonorInfo
                {
                    DonorId = donorId,
                    RegistryCode = RegistryCode.AN,
                    DonorType = DonorType.Adult
                },
                IsAvailableForSearch = true,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1))
            };

            await donorManagementService.ManageDonorBatchByAvailability(
                new[] { olderUnavailableUpdate, newerAvailableUpdate });

            await donorService
                .Received(0)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsUnavailableInLatest_SetsDonorAsUnavailable()
        {
            const int donorId = 789;

            var olderAvailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfo = new DonorInfo
                {
                    DonorId = donorId,
                    RegistryCode = RegistryCode.AN,
                    DonorType = DonorType.Adult
                },
                IsAvailableForSearch = true,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100))
            };

            var newerUnavailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1))
            };

            await donorManagementService.ManageDonorBatchByAvailability(
                new[] { olderAvailableUpdate, newerUnavailableUpdate });

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_MultipleUpdatesPerDonor_AndDonorIsUnavailableInLatest_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;

            var olderAvailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfo = new DonorInfo
                {
                    DonorId = donorId,
                    RegistryCode = RegistryCode.AN,
                    DonorType = DonorType.Adult
                },
                IsAvailableForSearch = true,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100))
            };

            var newerUnavailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                IsAvailableForSearch = false,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1))
            };

            await donorManagementService.ManageDonorBatchByAvailability(
                new[] { olderAvailableUpdate, newerUnavailableUpdate });

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsAvailable_AndUpdateIsNewerThanThatLastApplied_AddsOrUpdatesDonor()
        {
            const int donorId = 456;
            var newerTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var olderTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100));

            logRepository
                .GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>())
                .Returns(new[] {
                    new DonorManagementLog
                    {
                        DonorId = donorId,
                        LastUpdateDateTime = olderTimestamp
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new DonorInfo(),
                    IsAvailableForSearch = true,
                    UpdateDateTime = newerTimestamp
                }});

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsAvailable_AndUpdateIsOlderThanThatLastApplied_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 456;
            var newerTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var olderTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100));

            logRepository
                .GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>())
                .Returns(new[] { 
                    new DonorManagementLog
                    {
                        DonorId = donorId,
                        LastUpdateDateTime = newerTimestamp
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new DonorInfo(),
                    IsAvailableForSearch = true,
                    UpdateDateTime = olderTimestamp
                }});

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<DonorInfo>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsNotAvailable_AndUpdateIsNewerThanThatLastApplied_SetsDonorAsUnavailable()
        {
            const int donorId = 789;
            var newerTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var olderTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100));

            logRepository
                .GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>())
                .Returns(new[] {
                    new DonorManagementLog
                    {
                        DonorId = donorId,
                        LastUpdateDateTime = olderTimestamp
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false,
                    UpdateDateTime = newerTimestamp
                }});

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_DonorIsNotAvailable_AndUpdateIsOlderThanThatLastApplied_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 789;
            var newerTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var olderTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100));

            logRepository
                .GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>())
                .Returns(new[] {
                    new DonorManagementLog
                    {
                        DonorId = donorId,
                        LastUpdateDateTime = newerTimestamp
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false,
                    UpdateDateTime = olderTimestamp
                }});

            await donorService
                .Received(0)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_SomeUpdatesOlderThanThoseLastApplied_LogsEventForEachNonApplicableUpdate()
        {
            const int donorId1 = 456;
            const int donorId2 = 789;
            const int donorId3 = 999;
            var newerTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
            var olderTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(100));

            logRepository
                .GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>())
                .Returns(new[] {
                    new DonorManagementLog
                    {
                        DonorId = donorId1,
                        LastUpdateDateTime = newerTimestamp
                    },
                    new DonorManagementLog
                    {
                        DonorId = donorId2,
                        LastUpdateDateTime = newerTimestamp
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId1,
                    UpdateDateTime = olderTimestamp
                },
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId2,
                    UpdateDateTime = olderTimestamp
                },
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId3
                }
            });

            // third donor update is applicable; only 2 events should be logged
            logger.Received(2).SendEvent(Arg.Any<DonorUpdateNotAppliedEventModel>());
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
                    DonorId = unavailableDonorId,
                    IsAvailableForSearch = false
                },
                new DonorAvailabilityUpdate
                {
                    DonorId = availableDonorId,
                    DonorInfo = new DonorInfo
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
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<DonorInfo>>(x =>
                    x.Single().DonorId == availableDonorId &&
                    x.Single().RegistryCode == registryCode &&
                    x.Single().DonorType == donorType));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_CreatesOrUpdatesDonorManagementLog()
        {
            const int donorId = 789;
            const int sequenceNumber = 123456789;
            var updateDateTime = DateTimeOffset.UtcNow;

            mapper.Map<IEnumerable<DonorManagementInfo>>(
                    Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(x => x.Single().DonorId == donorId))
                .Returns(new List<DonorManagementInfo>
                {
                    new DonorManagementInfo
                    {
                        DonorId = donorId,
                        UpdateSequenceNumber = sequenceNumber,
                        UpdateDateTime = updateDateTime
                    }
                });

            await donorManagementService.ManageDonorBatchByAvailability(
                new[] { new DonorAvailabilityUpdate { DonorId = donorId } });

            await logRepository
                .Received(1)
                .CreateOrUpdateDonorManagementLogBatch(Arg.Is<IEnumerable<DonorManagementInfo>>(x =>
                    x.Single().DonorId == donorId &&
                    x.Single().UpdateSequenceNumber == sequenceNumber &&
                    x.Single().UpdateDateTime == updateDateTime));
        }
    }
}
