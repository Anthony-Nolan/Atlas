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

        private static string GetDonorIdFromDonorInfoFunc(ServiceBusMessage<SearchableDonorUpdateModel> donor)
            => donor.DeserializedBody?.DonorId;

        private static FailedDonorInfo GetFailedDonorInfoFunc(ServiceBusMessage<SearchableDonorUpdateModel> donor)
            => new FailedDonorInfo(donor) { DonorId = GetDonorIdFromDonorInfoFunc(donor) };

        private static Task<DonorAvailabilityUpdate> ProcessDonorFunc(ServiceBusMessage<SearchableDonorUpdateModel> donor)
            => Task.FromResult(new DonorAvailabilityUpdate
            {
                DonorId = donor.DeserializedBody == null
                        ? default
                        : int.Parse(GetDonorIdFromDonorInfoFunc(donor))
            });

        private static Task<DonorAvailabilityUpdate> ThrowAnticipatedExceptionFunc(ServiceBusMessage<SearchableDonorUpdateModel> donor)
            => throw new ValidationException("failed");

        private static DonorInfoValidationFailureEventModel GetEventModelFunc(DonorProcessingException<ValidationException> ex)
            => new DonorInfoValidationFailureEventModel(ex);

        #endregion

        // Arbitrary implementation selected to test base functionality.
        private IDonorBatchProcessor<
            ServiceBusMessage<SearchableDonorUpdateModel>,
            DonorAvailabilityUpdate,
            ValidationException> donorBatchProcessor;

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
                ProcessDonorFunc,
                GetEventModelFunc,
                GetFailedDonorInfoFunc
            );

            result.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsEmptyList()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                ThrowAnticipatedExceptionFunc,
                GetEventModelFunc,
                GetFailedDonorInfoFunc
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
                GetEventModelFunc,
                GetFailedDonorInfoFunc
            );

            logger.Received(2).SendEvent(Arg.Any<DonorInfoValidationFailureEventModel>());
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
                GetEventModelFunc,
                GetFailedDonorInfoFunc
            );

            await notificationsClient.Received(1).SendAlert(Arg.Is<Alert>(x =>
                x.Description.Contains($"{donorId1}") &&
                x.Description.Contains($"{donorId2}")));
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_AndDonorHasNoId_AlertContainsMissingDonorIdText()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel()
                    }
                },
                ThrowAnticipatedExceptionFunc,
                GetEventModelFunc,
                GetFailedDonorInfoFunc
            );

            await notificationsClient.Received(1).SendAlert(Arg.Is<Alert>(x =>
                x.Description.ToLower().Contains("donor(s) without id")));
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotLogEvent()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                ProcessDonorFunc,
                GetEventModelFunc,
                GetFailedDonorInfoFunc
            );

            logger.DidNotReceive().SendEvent(Arg.Any<DonorInfoValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotSendAlert()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                ProcessDonorFunc,
                GetEventModelFunc,
                GetFailedDonorInfoFunc
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
                ProcessDonorFunc,
                GetEventModelFunc,
                GetFailedDonorInfoFunc
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
                    ? await ProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetEventModelFunc,
                GetFailedDonorInfoFunc
            );

            logger.Received(1).SendEvent(Arg.Any<DonorInfoValidationFailureEventModel>());
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
                    ? await ProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetEventModelFunc,
                GetFailedDonorInfoFunc
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
                    ? await ProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetEventModelFunc,
                GetFailedDonorInfoFunc
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
                        GetEventModelFunc,
                        GetFailedDonorInfoFunc
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
                        GetEventModelFunc,
                        GetFailedDonorInfoFunc
                    );
                }
            );
        }
    }
}
