using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Services.DonorManagement;
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

        private IMessageProcessor<SearchableDonorUpdateModel> messageProcessorService;
        private IDonorManagementService donorManagementService;
        private IDonorUpdateProcessor donorUpdateProcessor;

        [SetUp]
        public void Setup()
        {
            messageProcessorService = Substitute.For<IMessageProcessor<SearchableDonorUpdateModel>>();
            donorManagementService = Substitute.For<IDonorManagementService>();
            var logger = Substitute.For<ILogger>();
            donorUpdateProcessor = new DonorUpdateProcessor(messageProcessorService, donorManagementService, logger, BatchSize);
        }

        [Test]
        public async Task ProcessDonorUpdates_ProcessesMessageBatch()
        {
            await donorUpdateProcessor.ProcessDonorUpdates();

            await messageProcessorService.Received(1).ProcessMessageBatch(
                BatchSize,
                Arg.Any<Func<IEnumerable<ServiceBusMessage<SearchableDonorUpdateModel>>,Task>>());
        }
    }
}