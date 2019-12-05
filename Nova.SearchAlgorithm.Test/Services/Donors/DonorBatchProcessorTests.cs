using FluentAssertions;
using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Exceptions;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.Notifications;
using Nova.Utils.ServiceBus.Models;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ILogger = Nova.Utils.ApplicationInsights.ILogger;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class DonorBatchProcessorTests
    {
        #region Function delegates to pass into service under test

        private static readonly Func<ServiceBusMessage<SearchableDonorUpdateModel>, string> GetDefaultDonorIdFunc =
            donor => "id";

        private static readonly Func<ServiceBusMessage<SearchableDonorUpdateModel>, string> GetDonorIdFromDonorInfoFunc =
            donor => donor.DeserializedBody?.DonorId;

        private static readonly Func<ServiceBusMessage<SearchableDonorUpdateModel>, Task<DonorAvailabilityUpdate>> DefaultProcessDonorFunc =
                d => Task.FromResult(new DonorAvailabilityUpdate
                {
                    DonorId = d.DeserializedBody == null
                        ? default
                        : int.Parse(GetDonorIdFromDonorInfoFunc(d))
                });

        private static readonly Func<ServiceBusMessage<SearchableDonorUpdateModel>, Task<DonorAvailabilityUpdate>> ThrowAnticipatedExceptionFunc =
            d => throw new DonorUpdateValidationException(d, new ValidationException("failed"));

        private static readonly Func<DonorUpdateValidationException, ServiceBusMessage<SearchableDonorUpdateModel>, DonorUpdateFailureEventModel> GetDefaultEventModelFunc =
            (e, d) => new DonorUpdateFailureEventModel(e, "id");

        #endregion

        // Arbitrary implementation selected to test base functionality.
        private IDonorBatchProcessor<
            ServiceBusMessage<SearchableDonorUpdateModel>,
            DonorAvailabilityUpdate,
            DonorUpdateValidationException> donorBatchProcessor;

        private ILogger logger;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();
            donorBatchProcessor = new SearchableDonorUpdateConverter(logger, notificationsClient);
        }

        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyList()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>(),
                DefaultProcessDonorFunc,
                GetDefaultEventModelFunc,
                GetDefaultDonorIdFunc
            );

            result.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsEmptyList()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                ThrowAnticipatedExceptionFunc,
                GetDefaultEventModelFunc,
                GetDefaultDonorIdFunc
            );

            result.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_LogsOneEventPerFailure()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>(),
                    new ServiceBusMessage<SearchableDonorUpdateModel>()
                },
                ThrowAnticipatedExceptionFunc,
                GetDefaultEventModelFunc,
                GetDefaultDonorIdFunc
            );

            logger.Received(2).SendEvent(Arg.Any<DonorUpdateFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_SendsOneAlertListingFailedDonorIds()
        {
            const string donorId1 = "123";
            const string donorId2 = "456";

            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = donorId1 }
                    },
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = donorId2 }
                    }
                },
                ThrowAnticipatedExceptionFunc,
                GetDefaultEventModelFunc,
                GetDonorIdFromDonorInfoFunc
            );

            await notificationsClient.Received(1).SendAlert(Arg.Is<Alert>(x =>
                x.Description.Contains($"{donorId1}") &&
                x.Description.Contains($"{donorId2}")));
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotLogEvent()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                DefaultProcessDonorFunc,
                GetDefaultEventModelFunc,
                GetDefaultDonorIdFunc
            );

            logger.DidNotReceive().SendEvent(Arg.Any<DonorUpdateFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotSendAlert()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                DefaultProcessDonorFunc,
                GetDefaultEventModelFunc,
                GetDefaultDonorIdFunc
            );

            await notificationsClient.DidNotReceive().SendAlert(Arg.Any<Alert>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_ReturnsExpectedCollection()
        {
            const int donorId = 123;

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel { DonorId = donorId.ToString() }
                    }
                },
                DefaultProcessDonorFunc,
                GetDefaultEventModelFunc,
                GetDonorIdFromDonorInfoFunc
            );

            // The result will depend on the processDonorInfoFunc and the return type defined in the concrete class
            result.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_LogsOneEvent()
        {
            const string successfulDonor = "123";
            const string failedDonor = "456";

            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = successfulDonor }
                    },
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = failedDonor }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor
                    ? await DefaultProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetDefaultEventModelFunc,
                GetDefaultDonorIdFunc
            );

            logger.Received(1).SendEvent(Arg.Any<DonorUpdateFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_SendsOneAlertOnlyListingFailedDonorId()
        {
            const string successfulDonor = "123";
            const string failedDonor = "456";

            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = successfulDonor }
                    },
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = failedDonor }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor
                    ? await DefaultProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetDefaultEventModelFunc,
                GetDonorIdFromDonorInfoFunc
            );

            await notificationsClient.Received(1).SendAlert(Arg.Is<Alert>(x =>
                x.Description.Contains($"{failedDonor}") &&
                !x.Description.Contains($"{successfulDonor}")));
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_ReturnsExpectedCollection()
        {
            const int successfulDonor = 123;

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = successfulDonor.ToString() }
                    },
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = "failed-donor-id" }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor.ToString()
                    ? await DefaultProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetDefaultEventModelFunc,
                GetDonorIdFromDonorInfoFunc
            );

            // The result will depend on the return type defined in the concrete class
            result.Should().OnlyContain(d => d.DonorId == successfulDonor);
        }

        [Test]
        public void ProcessBatchAsync_AnticipatedFailure_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
                {
                    await donorBatchProcessor.ProcessBatchAsync(
                        new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                        ThrowAnticipatedExceptionFunc,
                        GetDefaultEventModelFunc,
                        GetDefaultDonorIdFunc
                    );
                }
            );
        }

        [Test]
        public void ProcessBatchAsync_UnanticipatedFailure_ThrowsException()
        {
            Assert.ThrowsAsync<Exception>(async () =>
                {
                    await donorBatchProcessor.ProcessBatchAsync(
                        new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                        d => throw new Exception("error"),
                        GetDefaultEventModelFunc,
                        GetDefaultDonorIdFunc
                    );
                }
            );
        }
    }
}
