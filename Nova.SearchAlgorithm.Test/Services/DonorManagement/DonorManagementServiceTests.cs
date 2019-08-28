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
        private IDonorManagementNotificationSender notificationSender;

        [SetUp]
        public void SetUp()
        {
            // All unit tests where logRepository's return object is not overridden will by default
            // test the scenario of "when donor has no log entries".
            logRepository = Substitute.For<IDonorManagementLogRepository>();
            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new DonorManagementLog[] { });

            var repositoryFactory = Substitute.For<IActiveRepositoryFactory>();
            repositoryFactory.GetDonorManagementLogRepository().Returns(logRepository);

            donorService = Substitute.For<IDonorService>();
            var logger = Substitute.For<ILogger>();
            mapper = Substitute.For<IMapper>();
            notificationSender = Substitute.For<IDonorManagementNotificationSender>();

            donorManagementService = new DonorManagementService(repositoryFactory, donorService, logger, mapper, notificationSender);
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
        public async Task ManageDonorBatchByAvailability_GetsDonorManagementLog()
        {
            const int donorId = 789;

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = 1
                }});

            await logRepository
                .Received(1)
                .GetDonorManagementLogBatch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateIsNewerThanLastUpdate_AndDonorIsAvailable_AddsOrUpdatesDonor()
        {
            const int donorId = 789;
            const long sequenceNumberOfLastUpdate = 123456789;
            const RegistryCode registryCode = RegistryCode.AN;
            const DonorType donorType = DonorType.Adult;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorManagementLog>
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumberOfLastUpdate + 1,
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
        public async Task ManageDonorBatchByAvailability_UpdateIsCoevalToLastUpdate_AndDonorIsAvailable_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;
            const long sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorManagementLog>
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumberOfLastUpdate,
                    IsAvailableForSearch = true
                }});

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateIsOlderThanLastUpdate_AndDonorIsAvailable_DoesNotAddOrUpdateDonor()
        {
            const int donorId = 789;
            const long sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorManagementLog>
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumberOfLastUpdate - 1,
                    IsAvailableForSearch = true
                }
            });

            await donorService
                .Received(0)
                .CreateOrUpdateDonorBatch(Arg.Any<IEnumerable<InputDonor>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateIsNewerThanLastUpdate_AndDonorIsNotAvailable_SetsDonorAsUnavailable()
        {
            const int donorId = 789;
            const long sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorManagementLog>
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumberOfLastUpdate + 1,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(1)
                .SetDonorBatchAsUnavailableForSearch(Arg.Is<IEnumerable<int>>(x => x.Single() == donorId));
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateIsCoevalToLastUpdate_AndDonorIsNotAvailable_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 789;
            const long sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorManagementLog>
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumberOfLastUpdate,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(0)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateIsOlderThanLastUpdate_AndDonorIsNotAvailable_DoesNotSetDonorAsUnavailable()
        {
            const int donorId = 789;
            const long sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new List<DonorManagementLog>
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            await donorManagementService.ManageDonorBatchByAvailability(new[] {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumberOfLastUpdate - 1,
                    IsAvailableForSearch = false
                }});

            await donorService
                .Received(0)
                .SetDonorBatchAsUnavailableForSearch(Arg.Any<IEnumerable<int>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateWasApplied_CreatesOrUpdatesDonorManagementLog()
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

            // no logs are returned by logRepository, so this update should be applied.
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

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateWasNotApplied_DoesNotCreateOrUpdateDonorManagementLog()
        {
            const int donorId = 789;
            const int sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new DonorManagementLog[]
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            // Update is older than last applied update and so should not be applied.
            await donorManagementService.ManageDonorBatchByAvailability(new[] {new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                UpdateSequenceNumber = sequenceNumberOfLastUpdate - 1
            }});

            await logRepository
                .Received(0)
                .CreateOrUpdateDonorManagementLogBatch(Arg.Any<IEnumerable<DonorManagementInfo>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateWasApplied_DoesNotSendNotification()
        {
            const int donorId = 789;
            const int sequenceNumber = 123456789;

            // no logs are returned by logRepository, so this update should be applied.
            await donorManagementService.ManageDonorBatchByAvailability(new[] {new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                UpdateSequenceNumber = sequenceNumber
            }});

            await notificationSender
                .Received(0)
                .SendDonorUpdatesNotAppliedNotification(Arg.Any<IEnumerable<DonorAvailabilityUpdate>>());
        }

        [Test]
        public async Task ManageDonorBatchByAvailability_UpdateWasNotApplied_SendsNotification()
        {
            const int donorId = 789;
            const int sequenceNumberOfLastUpdate = 123456789;

            logRepository.GetDonorManagementLogBatch(Arg.Any<IEnumerable<int>>()).Returns(new DonorManagementLog[]
            {
                new DonorManagementLog
                {
                    DonorId = donorId,
                    SequenceNumberOfLastUpdate = sequenceNumberOfLastUpdate
                }
            });

            // Update is older than last applied update and so should not be applied.
            await donorManagementService.ManageDonorBatchByAvailability(new[] {new DonorAvailabilityUpdate
            {
                DonorId = donorId,
                UpdateSequenceNumber = sequenceNumberOfLastUpdate - 1
            }});

            await notificationSender
                .Received(1)
                .SendDonorUpdatesNotAppliedNotification(Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(x =>
                    x.Single().DonorId == donorId &&
                    x.Single().UpdateSequenceNumber == sequenceNumberOfLastUpdate - 1));
        }
    }
}
