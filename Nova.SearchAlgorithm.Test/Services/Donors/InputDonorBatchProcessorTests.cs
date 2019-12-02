using FluentAssertions;
using FluentValidation;
using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.Utils.Notifications;
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
    public class InputDonorBatchProcessorTests
    {
        /// <summary>
        /// Arbitrary implementation selected to test base functionality.
        /// </summary>
        private IInputDonorBatchProcessor<InputDonor, ValidationException> donorBatchProcessor;

        private ILogger logger;
        private INotificationsClient notificationsClient;

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ILogger>();
            notificationsClient = Substitute.For<INotificationsClient>();
            donorBatchProcessor = new DonorValidator(logger, notificationsClient);
        }

        [Test]
        public async Task ProcessBatchAsync_NoDonors_ReturnsEmptyList()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor>(),
                Task.FromResult,
                (e, d) => new DonorValidationFailureEventModel(e, "id")
                );

            result.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_ReturnsEmptyList()
        {
            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor> { new InputDonor() },
                d => throw new ValidationException("failed"),
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            result.Should().BeEmpty();
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_LogsOneEventPerFailure()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor>
                {
                    new InputDonor(),
                    new InputDonor()
                },
                d => throw new ValidationException("failed"),
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            logger.Received(2).SendEvent(Arg.Any<DonorValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsFailProcessing_SendsOneAlertListingFailedDonorIds()
        {
            const int donorId1 = 123;
            const int donorId2 = 456;

            await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor>
                {
                    new InputDonor { DonorId = donorId1 },
                    new InputDonor { DonorId = donorId2 }
                },
                d => throw new ValidationException("failed"),
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            await notificationsClient.Received(1).SendAlert(Arg.Is<Alert>(x =>
                x.Description.Contains($"{donorId1}") &&
                x.Description.Contains($"{donorId2}")));
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotLogEvent()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor> { new InputDonor() },
                Task.FromResult,
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            logger.DidNotReceive().SendEvent(Arg.Any<DonorValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_DoesNotSendAlert()
        {
            await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor> { new InputDonor() },
                Task.FromResult,
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            await notificationsClient.DidNotReceive().SendAlert(Arg.Any<Alert>());
        }

        [Test]
        public async Task ProcessBatchAsync_AllDonorsProcessed_ReturnsExpectedCollection()
        {
            const int donorId = 123;

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor> { new InputDonor { DonorId = donorId } },
                Task.FromResult,
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            // The result will depend on the return type defined in the concrete class
            result.Should().OnlyContain(d => d.DonorId == donorId);
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_LogsOneEvent()
        {
            const int successfulDonor = 123;
            const int failedDonor = 456;

            await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor>
                {
                    new InputDonor { DonorId = successfulDonor },
                    new InputDonor { DonorId = failedDonor }
                },
                d => d.DonorId == successfulDonor
                    ? Task.FromResult(d)
                    : throw new ValidationException("failed"),
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            logger.Received(1).SendEvent(Arg.Any<DonorValidationFailureEventModel>());
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_SendsOneAlertOnlyListingFailedDonorId()
        {
            const int successfulDonor = 123;
            const int failedDonor = 456;

            await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor>
                {
                    new InputDonor { DonorId = successfulDonor },
                    new InputDonor { DonorId = failedDonor }
                },
                d => d.DonorId == successfulDonor
                    ? Task.FromResult(d)
                    : throw new ValidationException("failed"),
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            await notificationsClient.Received(1).SendAlert(Arg.Is<Alert>(x =>
                x.Description.Contains($"{failedDonor}") &&
                !x.Description.Contains($"{successfulDonor}")));
        }

        [Test]
        public async Task ProcessBatchAsync_OneDonorProcessedAndOneFailed_ReturnsExpectedCollection()
        {
            const int successfulDonor = 123;
            const int failedDonor = 456;

            var result = await donorBatchProcessor.ProcessBatchAsync(
                new List<InputDonor>
                {
                    new InputDonor { DonorId = successfulDonor },
                    new InputDonor { DonorId = failedDonor }
                },
                d => d.DonorId == successfulDonor
                    ? Task.FromResult(d)
                    : throw new ValidationException("failed"),
                (e, d) => new DonorValidationFailureEventModel(e, "id")
            );

            // The result will depend on the return type defined in the concrete class
            result.Should().OnlyContain(d => d.DonorId == successfulDonor);
        }

        [Test]
        public void ProcessBatchAsync_AnticipatedFailure_DoesNotThrowException()
        {
            // Exception type as defined in the concrete class.
            var exception = new ValidationException("error");

            Assert.DoesNotThrowAsync(async () =>
                {
                    await donorBatchProcessor.ProcessBatchAsync(
                        new List<InputDonor> {new InputDonor()},
                        d => throw exception,
                        (e, d) => new DonorValidationFailureEventModel(e, "id")
                    );
                }
            );
        }

        [Test]
        public void ProcessBatchAsync_UnanticipatedFailure_ThrowsException()
        {
            var exception = new Exception("error");

            Assert.ThrowsAsync<Exception>(async () =>
                {
                    await donorBatchProcessor.ProcessBatchAsync(
                        new List<InputDonor> { new InputDonor() },
                        d => throw exception,
                        (e, d) => new DonorValidationFailureEventModel(e, "id")
                    );
                }
            );
        }
    }
}
