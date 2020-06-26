using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using NSubstitute;
using NUnit.Framework;

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

        [SetUp]
        public void Setup()
        {
            messageProcessorServiceForA = Substitute.For<IMessageProcessorForDbADonorUpdates>();
            messageProcessorServiceForB = Substitute.For<IMessageProcessorForDbBDonorUpdates>();
            var refreshHistory = Substitute.For<IDataRefreshHistoryRepository>();
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
        public async Task ProcessDonorUpdates_ProcessesMessageBatch()
        {
            messageProcessorServiceForA.ClearReceivedCalls();

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(TransientDatabase.DatabaseA);

            await messageProcessorServiceForA.Received(1).ProcessMessageBatchAsync(
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                BatchSize,
                Arg.Any<int>());
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatchFromCorrectFeed()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            messageProcessorServiceForB.ClearReceivedCalls();

            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(TransientDatabase.DatabaseB);

            await messageProcessorServiceForA.DidNotReceiveWithAnyArgs().ProcessMessageBatchAsync(default, default);
            await messageProcessorServiceForB.ReceivedWithAnyArgs().ProcessMessageBatchAsync(default, default);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch_WithPrefetchCountGreaterThanBatchSize()
        {
            messageProcessorServiceForA.ClearReceivedCalls();
            
            await donorUpdateProcessor.ProcessDifferentialDonorUpdates(TransientDatabase.DatabaseA);

            await messageProcessorServiceForA.Received(1).ProcessMessageBatchAsync(
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                BatchSize,
                Arg.Is<int>(b => b > BatchSize));
        }
    }
}