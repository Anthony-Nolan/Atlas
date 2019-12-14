using Microsoft.Extensions.DependencyInjection;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using Nova.Utils.ServiceBus.BatchReceiving;
using Nova.Utils.ServiceBus.Models;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Services.Donors;
using ILogger = Nova.Utils.ApplicationInsights.ILogger;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests
{
    /// <summary>
    /// Tests the processing and validation of donor update messages.
    /// Fixture does not go so far as to check that updates reach the database,
    /// as that is covered by other integration tests, e.g., donor service tests.
    /// Only a few invalid scenarios are included here to ensure validation is taking place;
    /// validator logic is extensively covered by unit tests.
    /// </summary>
    [TestFixture]
    public class DonorUpdateProcessorTests
    {
        private IDonorUpdateProcessor donorUpdateProcessor;

        private IServiceBusMessageReceiver<SearchableDonorUpdateModel> messageReceiver;
        private IMessageProcessor<SearchableDonorUpdateModel> messageProcessor;
        private IDonorManagementService donorManagementService;
        private ISearchableDonorUpdateConverter searchableDonorUpdateConverter;
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;
        private ILogger logger;
        private int batchSize;

        [SetUp]
        public void SetUp()
        {
            var provider = DependencyInjection.DependencyInjection.Provider;

            messageReceiver = Substitute.For<IServiceBusMessageReceiver<SearchableDonorUpdateModel>>();
            messageProcessor = new MessageProcessor<SearchableDonorUpdateModel>(messageReceiver);
            
            donorManagementService = Substitute.For<IDonorManagementService>();
            searchableDonorUpdateConverter = provider.GetService<ISearchableDonorUpdateConverter>();
            failedDonorsNotificationSender = Substitute.For<IFailedDonorsNotificationSender>();
            logger = Substitute.For<ILogger>();

            batchSize = 10;

            donorUpdateProcessor = new DonorUpdateProcessor(
                messageProcessor,
                donorManagementService,
                searchableDonorUpdateConverter,
                failedDonorsNotificationSender,
                logger,
                batchSize
                );
        }

        [Test]
        public async Task ProcessDonorUpdates_SingleUpdateHasValidRequiredDonorInfo_ManagesDonorUpdate()
        {
            var message = SearchableDonorUpdateMessageBuilder.New.Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>> { message });

            await donorUpdateProcessor.ProcessDonorUpdates();

            await donorManagementService.Received().ManageDonorBatchByAvailability(
                Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(
                    x => x.Count() == 1));
        }

        [Test]
        public async Task ProcessDonorUpdates_SingleUpdateHasInvalidDonorInfo_DoesNotManageDonorUpdate()
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.DonorType, "invalid-donor-type")
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId.ToString());

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>> { message });

            await donorUpdateProcessor.ProcessDonorUpdates();

            await donorManagementService.DidNotReceive().ManageDonorBatchByAvailability(
                Arg.Any<IEnumerable<DonorAvailabilityUpdate>>());
        }

        [Test]
        public void ProcessDonorUpdates_SingleUpdateHasInvalidDonorInfo_DoesNotThrowValidationException()
        {
            var donorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.DonorType, "invalid-donor-type")
                .Build();

            var update = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, donorInfo)
                .With(x => x.DonorId, donorInfo.DonorId.ToString());

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>> { message });

            Assert.DoesNotThrowAsync(async () => await donorUpdateProcessor.ProcessDonorUpdates());
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
                .With(x => x.DonorId, donorInfo.DonorId.ToString());

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>> { message });

            await donorUpdateProcessor.ProcessDonorUpdates();

            await donorManagementService.DidNotReceive().ManageDonorBatchByAvailability(
                Arg.Any<IEnumerable<DonorAvailabilityUpdate>>());
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
                .With(x => x.DonorId, donorInfo.DonorId.ToString());

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>> { message });

            Assert.DoesNotThrowAsync(async () => await donorUpdateProcessor.ProcessDonorUpdates());
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
                .With(x => x.DonorId, donorInfo.DonorId.ToString());

            var message = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, update)
                .Build();
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>> { message });

            await donorUpdateProcessor.ProcessDonorUpdates();

            await donorManagementService.Received().ManageDonorBatchByAvailability(
                Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(
                    x => x.Count() == 1));
        }

        [Test]
        public async Task ProcessDonorUpdates_MultipleUpdates_AllValid_ManagesAllDonorUpdates()
        {
            const int updateCount = 3;
            var messages = SearchableDonorUpdateMessageBuilder.New.Build(updateCount);
            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(messages);

            await donorUpdateProcessor.ProcessDonorUpdates();

            await donorManagementService.Received().ManageDonorBatchByAvailability(
                Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(
                    x => x.Count() == updateCount));
        }

        [Test]
        public async Task ProcessDonorUpdates_MultipleUpdates_OneValid_OnlyManagesOneDonorUpdate()
        {
            var validMessage = SearchableDonorUpdateMessageBuilder.New.Build();

            var invalidDonorInfo = SearchableDonorInformationBuilder.New
                .With(x => x.RegistryCode, "invalid-registry")
                .Build();

            var invalidUpdate = SearchableDonorUpdateBuilder.New
                .With(x => x.SearchableDonorInformation, invalidDonorInfo)
                .With(x => x.DonorId, invalidDonorInfo.DonorId.ToString());

            var invalidMessage = SearchableDonorUpdateMessageBuilder.New
                .With(x => x.DeserializedBody, invalidUpdate)
                .Build();

            messageReceiver
                .ReceiveMessageBatchAsync(Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    validMessage, invalidMessage
                });

            await donorUpdateProcessor.ProcessDonorUpdates();

            await donorManagementService.Received().ManageDonorBatchByAvailability(
                Arg.Is<IEnumerable<DonorAvailabilityUpdate>>(
                    x => x.Count() == 1));
        }
    }
}