using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.ServiceBus.Models;
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
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using static Atlas.MatchingAlgorithm.Data.Persistent.Models.TransientDatabase;

namespace Atlas.MatchingAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class DonorUpdateProcessorTests
    {
        private const int BatchSize = 100;

        private IMessageProcessorForDbADonorUpdates messageProcessorServiceForA;
        private IMessageProcessorForDbBDonorUpdates messageProcessorServiceForB;
        private IDonorManagementService donorManagementService;
        private IDonorUpdateProcessor donorUpdateProcessor;
        private ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private IDataRefreshHistoryRepository refreshHistory;
        private IDonorReader donorReader;
        private IDonorUpdatesSaver donorUpdatesSaver;

        private readonly DataRefreshRecord dbARefreshing = DataRefreshRecordBuilder.New.WithDatabase(DatabaseA);
        private readonly DataRefreshRecord dbBRefreshing = DataRefreshRecordBuilder.New.WithDatabase(DatabaseB);

        [SetUp]
        public void Setup()
        {
            messageProcessorServiceForA = Substitute.For<IMessageProcessorForDbADonorUpdates>();
            messageProcessorServiceForB = Substitute.For<IMessageProcessorForDbBDonorUpdates>();

            refreshHistory = Substitute.For<IDataRefreshHistoryRepository>();
            refreshHistory.GetIncompleteRefreshJobs().Returns(Enumerable.Empty<DataRefreshRecord>());

            donorManagementService = Substitute.For<IDonorManagementService>();
            searchableDonorUpdateConverter = Substitute.For<ISearchableDonorUpdateConverter>();
            var hlaVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();
            ConfigureMocksToPassThroughToDonorService();

            var logger = Substitute.For<IMatchingAlgorithmImportLogger>();

            donorReader = Substitute.For<IDonorReader>();
            donorUpdatesSaver = Substitute.For<IDonorUpdatesSaver>();

            donorUpdateProcessor = new DonorUpdateProcessor(
                messageProcessorServiceForA,
                messageProcessorServiceForB,
                refreshHistory,
                donorManagementService,
                searchableDonorUpdateConverter,
                hlaVersionAccessor,
                new DonorManagementSettings {BatchSize = BatchSize, OngoingDifferentialDonorUpdatesShouldBeFullyTransactional = false},
                logger,
                new MatchingAlgorithmImportLoggingContext(),
                donorReader,
                donorUpdatesSaver);

            messageProcessorServiceForA.ClearReceivedCalls();
            messageProcessorServiceForB.ClearReceivedCalls();
            donorManagementService.ClearReceivedCalls();
        }

        private void ConfigureMocksToPassThroughToDonorService()
        {
            searchableDonorUpdateConverter.ConvertSearchableDonorUpdatesAsync(default).ReturnsForAnyArgs(
                Task.FromResult(new DonorBatchProcessingResult<DonorAvailabilityUpdate>(
                    new List<DonorAvailabilityUpdate> {new DonorAvailabilityUpdate()}
                ))
            );
            ConfigureMockMessageProcessorToPassThrough(messageProcessorServiceForA);
            ConfigureMockMessageProcessorToPassThrough(messageProcessorServiceForB);
        }

        private void ConfigureMockMessageProcessorToPassThrough(IMessageProcessor<SearchableDonorUpdate> messageProcessorMock)
        {
            messageProcessorMock
                .ProcessAllMessagesInBatches_Async(default, default)
                .ReturnsForAnyArgs(Task.CompletedTask)
                .AndDoes(args =>
                {
                    var processingAction =
                        args.Arg<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>();
                    var blankInput = Enumerable.Empty<ServiceBusMessage<SearchableDonorUpdate>>();
                    processingAction(blankInput);
                });
        }

        [Test]
        public async Task ProcessDonorUpdates_IfTargetDbIsActive_ThenProcessesMessages()
        {
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().NotBeEmpty();
        }

        [Test]
        public async Task ProcessDonorUpdates_IfTargetDbIsDormant_ThenProcessesMessages_ButDiscardsThem()
        {
            refreshHistory.GetActiveDatabase().Returns(DatabaseB);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public async Task ProcessDonorUpdates_IfTargetDbIsRefreshing_ThenDoesNotProcessMessages()
        {
            refreshHistory.GetIncompleteRefreshJobs().Returns(new[] {dbARefreshing});
            refreshHistory.GetActiveDatabase().Returns(DatabaseB);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public async Task ProcessDonorUpdates_IfOtherDbIsRefreshing_ThenProcessesMessages()
        {
            refreshHistory.GetIncompleteRefreshJobs().Returns(new[] {dbBRefreshing});
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().NotBeEmpty();
        }

        [Test]
        public async Task ProcessDonorUpdates_IfTargetDbIsPerformingInitialRefresh_ThenDoesNotProcessMessages()
        {
            refreshHistory.GetIncompleteRefreshJobs().Returns(new[] {dbARefreshing});
            refreshHistory.GetActiveDatabase().Returns((TransientDatabase?) null);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public async Task ProcessesDonorUpdates_IfOtherDbIsPerformingInitialRefresh_ThenProcessesMessages_ButDiscardsThem()
        {
            refreshHistory.GetIncompleteRefreshJobs().Returns(new[] {dbBRefreshing});
            refreshHistory.GetActiveDatabase().Returns((TransientDatabase?) null);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public async Task ProcessesDonorUpdates_IfHistoryStateIsBlank_ThenDoesNotProcessMessages()
        {
            refreshHistory.GetIncompleteRefreshJobs().Returns(Enumerable.Empty<DataRefreshRecord>());
            refreshHistory.GetActiveDatabase().Returns((TransientDatabase?) null);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public async Task ProcessesDonorUpdates_IfHistoryStateIsUnexpected_ThenDoesNotProcessMessages()
        {
            refreshHistory.GetIncompleteRefreshJobs().Returns(new[] {dbARefreshing, dbBRefreshing});
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
            donorManagementService.ReceivedCalls().Should().BeEmpty();
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatchFromCorrectFeed_OnDbA()
        {
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForB.DidNotReceiveWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
            await messageProcessorServiceForA.ReceivedWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatchFromCorrectFeed_OnDbB()
        {
            refreshHistory.GetActiveDatabase().Returns(DatabaseB);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseB);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
            await messageProcessorServiceForB.ReceivedWithAnyArgs().ProcessAllMessagesInBatches_Async(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessages_InBatches()
        {
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.Received(1).ProcessAllMessagesInBatches_Async(
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                BatchSize,
                Arg.Any<int>());
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch_WithPrefetchCountGreaterThanBatchSize()
        {
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.Received(1).ProcessAllMessagesInBatches_Async(
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                BatchSize,
                Arg.Is<int>(b => b > BatchSize));
        }

        [Test]
        public async Task ProcessDeadLetterDifferentialDonorUpdates_DonorExists_ShouldBeAvailableForSearch()
        {
            var searchableDonorUpdates = new SearchableDonorUpdate[]
            {
                new () 
                {
                    DonorId = 1,
                    SearchableDonorInformation = new SearchableDonorInformation
                    {
                        A_1 = "01:01"
                    }
                }
            };

            donorReader.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, Donor>
            {
                { 1, new Donor { A_1 = "01:02" } }
            });

            await donorUpdateProcessor.ProcessDeadLetterDifferentialDonorUpdates(searchableDonorUpdates);

            await donorReader.Received(1).GetDonors(Arg.Any<IEnumerable<int>>());
            await donorUpdatesSaver.Received(1).Save(Arg.Is<IReadOnlyCollection<SearchableDonorUpdate>>(l =>
                l.First().DonorId == 1 && l.First().SearchableDonorInformation.A_1 == "01:02" && l.First().IsAvailableForSearch));
        }

        [Test]
        public async Task ProcessDeadLetterDifferentialDonorUpdates_DonorDoNotExist_ShouldNotBeAvailableForSearch()
        {
            var searchableDonorUpdates = new SearchableDonorUpdate[]
            {
                new ()
                {
                    DonorId = 2,
                    SearchableDonorInformation = new SearchableDonorInformation
                    {
                        A_1 = "01:01"
                    }
                },
            };

            donorReader.GetDonors(Arg.Any<IEnumerable<int>>()).Returns(new Dictionary<int, Donor>
            {
                { 1, new Donor { A_1 = "01:02" } }
            });

            await donorUpdateProcessor.ProcessDeadLetterDifferentialDonorUpdates(searchableDonorUpdates);

            await donorReader.Received(1).GetDonors(Arg.Any<IEnumerable<int>>());
            await donorUpdatesSaver.Received(1).Save(Arg.Is<IReadOnlyCollection<SearchableDonorUpdate>>(l =>
                l.First().DonorId == 2 && l.First().SearchableDonorInformation.A_1 == "01:01" && !l.First().IsAvailableForSearch));
        }
    }
}