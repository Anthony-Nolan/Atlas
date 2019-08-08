using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using Nova.SearchAlgorithm.Functions.DonorManagement.Services;
using Nova.SearchAlgorithm.Functions.DonorManagement.Services.ServiceBus;
using Nova.SearchAlgorithm.Services;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Test.Services
{
    [TestFixture]
    public class DonorUpdateProcessorTests
    {
        private const int BatchSize = 100;

        private IMessageProcessorService<SearchableDonorUpdateModel> messageProcessorService;
        private IDonorManagementService donorManagementService;
        private IDonorUpdateProcessor donorUpdateProcessor;

        [SetUp]
        public void Setup()
        {
            messageProcessorService = Substitute.For<IMessageProcessorService<SearchableDonorUpdateModel>>();
            donorManagementService = Substitute.For<IDonorManagementService>();
            donorUpdateProcessor = new DonorUpdateProcessor(messageProcessorService, donorManagementService, BatchSize);
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