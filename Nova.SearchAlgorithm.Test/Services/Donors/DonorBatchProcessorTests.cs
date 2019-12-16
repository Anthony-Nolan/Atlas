using FluentAssertions;
using FluentValidation;
using Nova.DonorService.Client.Models.DonorUpdate;
using Nova.SearchAlgorithm.ApplicationInsights.DonorProcessing;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.SearchAlgorithm.Services.Donors;
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
        private const string DefaultEventName = "event-name";

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

        #endregion

        // Arbitrary implementation selected to test base functionality.
        private IDonorBatchProcessor<ServiceBusMessage<SearchableDonorUpdateModel>, DonorAvailabilityUpdate> donorBatchProcessor;

        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            donorBatchProcessor = new SearchableDonorUpdateConverter(logger);
        }


        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyProcessingResults()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>(),
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.ProcessingResults.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyFailedDonors()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>(),
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsEmptyProcessingResults()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                ThrowAnticipatedExceptionFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.ProcessingResults.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsFailedDonors()
        {
            const string donorId = "donor-id";

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>()
                    {
                        DeserializedBody = new SearchableDonorUpdateModel
                        {
                            DonorId = donorId
                        }
                    }
                },
                ThrowAnticipatedExceptionFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().OnlyContain(d => d.DonorId == donorId);
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
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            logger.Received(2).SendEvent(Arg.Any<DonorInfoValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotLogEvent()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            logger.DidNotReceive().SendEvent(Arg.Any<DonorInfoValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_ReturnsExpectedProcessingResults()
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
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            // The result will depend on the processDonorInfoFunc and the return type defined in the concrete class
            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_ReturnsEmptyFailedDonors()
        {
            const string donorId = "123";

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel
                        {
                            DonorId = donorId
                        }
                    }
                },
                ProcessDonorFunc,
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().BeEmpty();
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
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            logger.Received(1).SendEvent(Arg.Any<DonorInfoValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_ReturnsProcessingResult()
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
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = "456" }
                    }
                },
                async d => d.DeserializedBody.DonorId == successfulDonor.ToString()
                    ? await ProcessDonorFunc(d)
                    : await ThrowAnticipatedExceptionFunc(d),
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            // The result will depend on the return type defined in the concrete class
            result.ProcessingResults.Should().OnlyContain(d => d.DonorId == successfulDonor);
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_ReturnsFailedDonor()
        {
            const string failedDonor = "123";

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<ServiceBusMessage<SearchableDonorUpdateModel>>
                {
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = "456" }
                    },
                    new ServiceBusMessage<SearchableDonorUpdateModel>
                    {
                        DeserializedBody = new SearchableDonorUpdateModel{ DonorId = failedDonor }
                    }
                },
                async d => d.DeserializedBody.DonorId == failedDonor
                    ? await ThrowAnticipatedExceptionFunc(d)
                    : await ProcessDonorFunc(d),
                GetFailedDonorInfoFunc,
                DefaultEventName
            );

            result.FailedDonors.Should().OnlyContain(d => d.DonorId == failedDonor);
        }

        [Test]
        public void ProcessBatchAsync_AnticipatedFailure_DoesNotThrowException()
        {
            Assert.DoesNotThrowAsync(async () =>
            {
                await donorBatchProcessor.ProcessBatchAsync(
                    new List<ServiceBusMessage<SearchableDonorUpdateModel>> { new ServiceBusMessage<SearchableDonorUpdateModel>() },
                    ThrowAnticipatedExceptionFunc,
                    GetFailedDonorInfoFunc,
                    DefaultEventName
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
                    GetFailedDonorInfoFunc,
                    DefaultEventName
                );
            }
            );
        }
    }
}