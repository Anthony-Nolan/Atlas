using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using AutoMapper;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class DonorManagementServiceTests
    {
        private IDonorManagementLogRepository logRepository;
        private IDonorService donorService;
        private IDonorManagementService donorManagementService;
        private IMatchingAlgorithmImportLogger logger;

        [SetUp]
        public void SetUp()
        {
            logRepository = Substitute.For<IDonorManagementLogRepository>();
            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new DonorManagementLog[] { });

            var repositoryFactory = Substitute.For<IStaticallyChosenDatabaseRepositoryFactory>();
            repositoryFactory.GetDonorManagementLogRepositoryForDatabase(default).ReturnsForAnyArgs(logRepository);

            donorService = Substitute.For<IDonorService>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            
            donorManagementService = new DonorManagementService(repositoryFactory, donorService, logger);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsAvailableForSearch_AddsOrUpdatesDonor()
        {
            const int donorId = 456;
            const DonorType donorType = DonorType.Adult;

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new DonorInfo
                    {
                        DonorId = donorId,
                        DonorType = donorType
                    },
                    IsAvailableForSearch = true
                }},
                default,
                default,
                default);

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<DonorInfo>>(x =>
                        x.Single().DonorId == donorId &&
                        x.Single().DonorType == donorType),
                    Arg.Any<TransientDatabase>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>());
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsAvailableForSearch_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 456;

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new DonorInfo
                    {
                        DonorId = donorId,
                        DonorType = DonorType.Adult
                    },
                    IsAvailableForSearch = true
                }},
                default,
                default,
                default);

            await donorService
                .DidNotReceiveWithAnyArgs()
                .SetDonorBatchAsUnavailableForSearch(default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsNotAvailableForSearch_SetsDonorAsUnavailable()
        {
            const int donorId = 789;

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false
                }},
                default,
                default,
                default);

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(
                    Arg.Is<List<int>>(x => x.Single() == donorId),
                    Arg.Any<TransientDatabase>());
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsNotAvailableForSearch_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false
                }},
                default,
                default,
                default);

            await donorService
                .DidNotReceiveWithAnyArgs()
                .CreateOrUpdateDonorBatch(default, default, default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_MultipleUpdatesPerDonor_AndDonorIsAvailableInLatest_AddsOrUpdatesDonor()
        {
            const int donorId = 456;
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
                    DonorType = donorType
                },
                IsAvailableForSearch = true,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1))
            };

            await donorManagementService.ApplyDonorUpdatesToDatabase(
                new[] { olderUnavailableUpdate, newerAvailableUpdate },
                default,
                default,
                default);

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<DonorInfo>>(x =>
                        x.Single().DonorId == donorId &&
                        x.Single().DonorType == donorType),
                    Arg.Any<TransientDatabase>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>());
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_MultipleUpdatesPerDonor_AndDonorIsAvailableInLatest_DoesNotSetDonorAsUnavailable()
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
                    DonorType = DonorType.Adult
                },
                IsAvailableForSearch = true,
                UpdateDateTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1))
            };

            await donorManagementService.ApplyDonorUpdatesToDatabase(
                new[] { olderUnavailableUpdate, newerAvailableUpdate },
                default,
                default,
                default);

            await donorService
                .DidNotReceiveWithAnyArgs()
                .SetDonorBatchAsUnavailableForSearch(default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_MultipleUpdatesPerDonor_AndDonorIsUnavailableInLatest_SetsDonorAsUnavailable()
        {
            const int donorId = 789;

            var olderAvailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfo = new DonorInfo
                {
                    DonorId = donorId,
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(
                new[] { olderAvailableUpdate, newerUnavailableUpdate },
                default,
                default,
                default);

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(
                    Arg.Is<List<int>>(x => x.Single() == donorId),
                    Arg.Any<TransientDatabase>());
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_MultipleUpdatesPerDonor_AndDonorIsUnavailableInLatest_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;

            var olderAvailableUpdate = new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                DonorInfo = new DonorInfo
                {
                    DonorId = donorId,
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(
                new[] { olderAvailableUpdate, newerUnavailableUpdate },
                default,
                default,
                default);

            await donorService
                .DidNotReceiveWithAnyArgs()
                .CreateOrUpdateDonorBatch(default, default, default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsAvailable_AndUpdateIsNewerThanThatLastApplied_AddsOrUpdatesDonor()
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new DonorInfo(),
                    IsAvailableForSearch = true,
                    UpdateDateTime = newerTimestamp
                }},
                default,
                default,
                default);

            await donorService
                .ReceivedWithAnyArgs(1)
                .CreateOrUpdateDonorBatch(default, default, default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsAvailable_AndUpdateIsOlderThanThatLastApplied_DoesNotAddOrUpdateDonor()
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    DonorInfo = new DonorInfo(),
                    IsAvailableForSearch = true,
                    UpdateDateTime = olderTimestamp
                }},
                default,
                default,
                default);

            await donorService
                .DidNotReceiveWithAnyArgs()
                .CreateOrUpdateDonorBatch(default, default, default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsNotAvailable_AndUpdateIsNewerThanThatLastApplied_SetsDonorAsUnavailable()
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false,
                    UpdateDateTime = newerTimestamp
                }},
                default,
                default,
                default);

            await donorService
                .ReceivedWithAnyArgs(1)
                .SetDonorBatchAsUnavailableForSearch(default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_DonorIsNotAvailable_AndUpdateIsOlderThanThatLastApplied_DoesNotSetDonorAsUnavailable()
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    IsAvailableForSearch = false,
                    UpdateDateTime = olderTimestamp
                }},
                default,
                default,
                default);

            await donorService
                .DidNotReceiveWithAnyArgs()
                .SetDonorBatchAsUnavailableForSearch(default, default);
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_SomeUpdatesOlderThanThoseLastApplied_LogsEventForEachNonApplicableUpdate()
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

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
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
                }},
                default,
                default,
                default);

            // third donor update is applicable; only 2 events should be logged
            logger.Received(2).SendEvent(Arg.Any<DonorUpdateNotAppliedEventModel>());
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_UpdatesContainAvailableAndUnavailableDonors_ModifiesDonorsCorrectly()
        {
            const int availableDonorId = 123;
            const int unavailableDonorId = 456;
            const DonorType donorType = DonorType.Adult;

            await donorManagementService.ApplyDonorUpdatesToDatabase(new[] {
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
                        DonorType = donorType
                    },
                    IsAvailableForSearch = true
                }},
                default,
                default,
                default);

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(
                    Arg.Is<List<int>>(x => x.Single() == unavailableDonorId),
                    Arg.Any<TransientDatabase>());

            await donorService
                .Received(1)
                .CreateOrUpdateDonorBatch(Arg.Is<IEnumerable<DonorInfo>>(x =>
                        x.Single().DonorId == availableDonorId &&
                        x.Single().DonorType == donorType),
                    Arg.Any<TransientDatabase>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>());
        }

        [Test]
        public async Task ApplyDonorUpdatesToDatabase_CreatesOrUpdatesDonorManagementLog()
        {
            const int donorId = 789;
            const int sequenceNumber = 123456789;
            var updateDateTime = DateTimeOffset.UtcNow;

            await donorManagementService.ApplyDonorUpdatesToDatabase(
                new[] { new DonorAvailabilityUpdate { DonorId = donorId, UpdateDateTime = updateDateTime, UpdateSequenceNumber = sequenceNumber} },
                default,
                default,
                default);

            await logRepository
                .Received(1)
                .CreateOrUpdateDonorManagementLogBatch(Arg.Is<IEnumerable<DonorManagementInfo>>(x =>
                    x.Single().DonorId == donorId &&
                    x.Single().UpdateSequenceNumber == sequenceNumber &&
                    x.Single().UpdateDateTime == updateDateTime));
        }
    }
}
