using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using EnumStringValues;
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

        private readonly DataRefreshRecord dbARefreshing = DataRefreshRecordBuilder.New.WithDatabase(DatabaseA);
        private readonly DataRefreshRecord dbBRefreshing = DataRefreshRecordBuilder.New.WithDatabase(DatabaseB);

        [SetUp]
        public void Setup()
        {
            messageProcessorServiceForA = Substitute.For<IMessageProcessorForDbADonorUpdates>();
            messageProcessorServiceForB = Substitute.For<IMessageProcessorForDbBDonorUpdates>();

            refreshHistory = Substitute.For<IDataRefreshHistoryRepository>();
            refreshHistory.GetInProgressJobs().Returns(Enumerable.Empty<DataRefreshRecord>());

            donorManagementService = Substitute.For<IDonorManagementService>();
            searchableDonorUpdateConverter = Substitute.For<ISearchableDonorUpdateConverter>();
            var logger = Substitute.For<ILogger>();

            donorUpdateProcessor = new DonorUpdateProcessor(
                messageProcessorServiceForA,
                messageProcessorServiceForB,
                refreshHistory,
                donorManagementService,
                searchableDonorUpdateConverter,
                logger,
                BatchSize);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch_IfTargetDbIsActive()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch_IfTargetDbIsDormant()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetActiveDatabase().Returns(DatabaseB);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_DoesNotProcessMessages_IfTargetDbIsRefreshing()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetInProgressJobs().Returns(new [] { dbARefreshing });
            refreshHistory.GetActiveDatabase().Returns(DatabaseB);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessages_IfOtherDbIsRefreshing()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetInProgressJobs().Returns(new[] { dbBRefreshing });
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_DoesNotProcessMessages_IfTargetDbIsPerformingInitialRefresh()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetInProgressJobs().Returns(new[] { dbARefreshing });
            refreshHistory.GetActiveDatabase().Returns((TransientDatabase?)null);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessesDonorUpdates_ProcessesMessages_IfOtherDbIsPerformingInitialRefresh()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetInProgressJobs().Returns(new[] { dbBRefreshing });
            refreshHistory.GetActiveDatabase().Returns((TransientDatabase?)null);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.ReceivedWithAnyArgs(1).ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessesDonorUpdates_DoesNotProcessMessages_IfHistoryStateIsBlank()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetInProgressJobs().Returns(Enumerable.Empty<DataRefreshRecord>());
            refreshHistory.GetActiveDatabase().Returns((TransientDatabase?)null);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessesDonorUpdates_DoesNotProcessMessages_IfHistoryStateIsConfusing()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetInProgressJobs().Returns(new[] { dbARefreshing, dbBRefreshing });
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatchFromCorrectFeed_OnDbA()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            messageProcessorServiceForB.ClearReceivedCalls();
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForB.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
            await messageProcessorServiceForA.ReceivedWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatchFromCorrectFeed_OnDbB()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            messageProcessorServiceForB.ClearReceivedCalls();
            refreshHistory.GetActiveDatabase().Returns(DatabaseB);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseB);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
            await messageProcessorServiceForB.ReceivedWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessages_InBatches()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.Received(1).ProcessMessageBatchAsync(
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                BatchSize,
                Arg.Any<int>());
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch_WithPrefetchCountGreaterThanBatchSize()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            refreshHistory.GetActiveDatabase().Returns(DatabaseA);

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(DatabaseA);

            await messageProcessorServiceForA.Received(1).ProcessMessageBatchAsync(
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                BatchSize,
                Arg.Is<int>(b => b > BatchSize));
        }
    }
}