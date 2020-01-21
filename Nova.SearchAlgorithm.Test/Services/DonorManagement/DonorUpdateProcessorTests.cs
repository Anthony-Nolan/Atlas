using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.ServiceBus.BatchReceiving;
using Nova.Utils.ServiceBus.Models;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class DonorUpdateProcessorTests
    {
        private const int BatchSize = 100;

        private IMessageProcessor<SearchableDonorUpdate> messageProcessorService;
        private IDonorManagementService donorManagementService;
        private IDonorUpdateProcessor donorUpdateProcessor;
        private ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;

        [SetUp]
        public void Setup()
        {
            messageProcessorService = Substitute.For<IMessageProcessor<SearchableDonorUpdate>>();
            donorManagementService = Substitute.For<IDonorManagementService>();
            searchableDonorUpdateConverter = Substitute.For<ISearchableDonorUpdateConverter>();
            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();
            var logger = Substitute.For<ILogger>();

            donorUpdateProcessor = new DonorUpdateProcessor(
                messageProcessorService,
                donorManagementService,
                searchableDonorUpdateConverter,
                failedDonorsNotificationSender,
                logger,
                BatchSize);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch()
        {
            await donorUpdateProcessor.ProcessDonorUpdates();

            await messageProcessorService.Received(1).ProcessMessageBatch(
                BatchSize,
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                Arg.Any<int>());
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch_WithPrefetchCountGreaterThanBatchSize()
        {
            await donorUpdateProcessor.ProcessDonorUpdates();

            await messageProcessorService.Received(1).ProcessMessageBatch(
                Arg.Any<int>(),
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdate>>, Task>>(),
                Arg.Is<int>(b => b > BatchSize));
        }
    }
}