using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.ServiceBus;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services.DonorUpdates;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DonorUpdates
{
    /// <summary>
    /// Tests the processing and validation of donor update messages.
    /// Fixture does not go so far as to check that updates reach the database,
    /// as that is covered by other integration tests, e.g., donor service tests.
    /// Only a few invalid scenarios are included here to ensure validation is taking place;
    /// validator logic is extensively covered by unit tests.
    /// </summary>
    [TestFixture]
    public class DonorUpdateProcessorValidationTests
    {
        private readonly string invalidHlaAtRequiredLocus = null;

        private IDonorUpdateProcessor donorUpdateProcessor;

        private IServiceBusMessageReceiver<SearchableDonorUpdate> messageReceiver;
        private DonorUpdateMessageProcessor messageProcessor;
        private IDonorManagementService donorManagementService;
        private ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private IMatchingAlgorithmImportLogger logger;
        private IDonorReader donorReader;
        private IDonorUpdatesSaver donorUpdatesSaver;
        private int batchSize;
        private const TransientDatabase DbTarget = TransientDatabase.DatabaseA;

        [SetUp]
        public void SetUp()
        {
            var provider = DependencyInjection.DependencyInjection.Provider;

            messageReceiver = Substitute.For<IServiceBusMessageReceiver<SearchableDonorUpdate>>();
            messageProcessor = new DonorUpdateMessageProcessor(messageReceiver);

            var refreshHistoryRepository = Substitute.For<IDataRefreshHistoryRepository>();
            refreshHistoryRepository.GetActiveDatabase().Returns(DbTarget);
            refreshHistoryRepository.GetIncompleteRefreshJobs().Returns(Enumerable.Empty<DataRefreshRecord>());

            donorManagementService = Substitute.For<IDonorManagementService>();
            searchableDonorUpdateConverter = provider.GetService<ISearchableDonorUpdateConverter>();
            var hlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            logger = Substitute.For<IMatchingAlgorithmImportLogger>();
            donorReader = Substitute.For<IDonorReader>();
            donorUpdatesSaver = Substitute.For<IDonorUpdatesSaver>();

            batchSize = 10;

            donorUpdateProcessor = new DonorUpdateProcessor(
                messageProcessor,
                messageProcessor,
                refreshHistoryRepository,
                donorManagementService,
                searchableDonorUpdateConverter,
                hlaVersionAccessor,
                new DonorManagementSettings {BatchSize = batchSize, OngoingDifferentialDonorUpdatesShouldBeFullyTransactional = false},
                logger,
                new MatchingAlgorithmImportLoggingContext(),
                donorReader,
                donorUpdatesSaver);
        }

        [Test]
        public async Task ProcessDonorUpdates_SingleUpdateHasValidRequiredDonorInfo_ManagesDonorUpdate()
        {
            var message = SearchableDonorUpdateMessageBuilder.New.Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>> {message});

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget);

            await donorManagementService.Received().ApplyDonorUpdatesToDatabase(
                Arg.Is<IReadOnlyCollection<DonorAvailabilityUpdate>>(x => x.Count == 1),
                Arg.Any<TransientDatabase>(),
                Arg.Any<string>(),
                Arg.Any<bool>());
        }

        [Test]
        public async Task ProcessDonorUpdates_SingleUpdateHasInvalidDonorInfo_DoesNotManageDonorUpdate()
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.A_1, invalidHlaAtRequiredLocus)
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId);

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>> {message});

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget);

            await donorManagementService.DidNotReceiveWithAnyArgs().ApplyDonorUpdatesToDatabase(default, default, default, default);
        }

        [Test]
        public void ProcessDonorUpdates_SingleUpdateHasInvalidDonorInfo_DoesNotThrowValidationException()
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.A_1, invalidHlaAtRequiredLocus)
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId);

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>> {message});

            Assert.DoesNotThrowAsync(async () => await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget));
        }

        [TestCase(null)]
        [TestCase("")]
        public async Task ProcessDonorUpdates_SingleUpdateIsMissingRequiredHla_DoesNotManageDonorUpdate(string missingHla)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.A_1, missingHla)
                .With(x => x.A_2, missingHla)
                .With(x => x.B_1, missingHla)
                .With(x => x.B_2, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId);

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>> {message});

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget);

            await donorManagementService.DidNotReceiveWithAnyArgs().ApplyDonorUpdatesToDatabase(default, default, default, default);
        }

        [TestCase(null)]
        [TestCase("")]
        public void ProcessDonorUpdates_SingleUpdateIsMissingRequiredHla_DoesNotThrowValidationException(string missingHla)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.A_1, missingHla)
                .With(x => x.A_2, missingHla)
                .With(x => x.B_1, missingHla)
                .With(x => x.B_2, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .With(x => x.DRB1_1, missingHla)
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId);

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>> {message});

            Assert.DoesNotThrowAsync(async () => await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget));
        }

        [TestCase(null)]
        [TestCase("")]
        public async Task ProcessDonorUpdates_SingleUpdateIsMissingOptionalHla_ManagesDonorUpdate(string missingHla)
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.C_1, missingHla)
                .With(x => x.C_2, missingHla)
                .With(x => x.DPB1_1, missingHla)
                .With(x => x.DPB1_1, missingHla)
                .With(x => x.DQB1_1, missingHla)
                .With(x => x.DQB1_1, missingHla)
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId);

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>> {message});

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget);

            await donorManagementService.Received().ApplyDonorUpdatesToDatabase(
                Arg.Is<IReadOnlyCollection<DonorAvailabilityUpdate>>(x => x.Count == 1),
                Arg.Any<TransientDatabase>(),
                Arg.Any<string>(),
                Arg.Any<bool>());
        }

        [Test]
        public async Task ProcessDonorUpdates_MultipleUpdates_AllValid_ManagesAllDonorUpdates()
        {
            const int updateCount = 3;
            var messages = SearchableDonorUpdateMessageBuilder.New.Build(updateCount);
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(messages);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget);

            await donorManagementService.Received().ApplyDonorUpdatesToDatabase(
                Arg.Is<IReadOnlyCollection<DonorAvailabilityUpdate>>(x => x.Count == updateCount),
                Arg.Any<TransientDatabase>(),
                Arg.Any<string>(),
                Arg.Any<bool>());
        }

        [Test]
        public async Task ProcessDonorUpdates_MultipleUpdates_OneValid_OnlyManagesOneDonorUpdate()
        {
            var validMessage = SearchableDonorUpdateMessageBuilder.New.Build();

            var invalidDonorInfo = SearchableDonorInformationBuilder.New
                .With(d => d.A_1, invalidHlaAtRequiredLocus)
                .Build();

            var invalidUpdate = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, invalidDonorInfo)
                .With(x => x.DonorId, invalidDonorInfo.DonorId);

            var invalidMessage = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, invalidUpdate)
                .Build();

            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdate>>
                {
                    validMessage, invalidMessage
                });

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DbTarget);

            await donorManagementService.Received().ApplyDonorUpdatesToDatabase(
                Arg.Is<IReadOnlyCollection<DonorAvailabilityUpdate>>(x => x.Count == 1),
                Arg.Any<TransientDatabase>(),
                Arg.Any<string>(),
                Arg.Any<bool>());
        }
    }
}