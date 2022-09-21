using System.Threading.Tasks;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using NSubstitute;
using NUnit.Framework;
using Atlas.Common.ServiceBus.Models;
using System.Collections.Generic;
using System;
using System.Linq;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using FluentAssertions;
using FluentValidation;
using LochNessBuilder;
using NSubstitute.ExceptionExtensions;
using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Exceptions;

namespace Atlas.MatchPrediction.Test.Services
{
    [TestFixture]
    internal class MatchPredictionRequestRunnerTests
    {
        private const int BatchSize = 50;
        private const string DefaultRequestId = "request-id";

        private static readonly Builder<IdentifiedMatchPredictionRequest> DefaultRequestBuilder = Builder<IdentifiedMatchPredictionRequest>.New
            .With(x => x.Id, DefaultRequestId)
            .With(x => x.SingleDonorMatchProbabilityInput, new SingleDonorMatchProbabilityInput());

        private static readonly Builder<ServiceBusMessage<IdentifiedMatchPredictionRequest>> DefaultMessageBuilder = Builder<ServiceBusMessage<IdentifiedMatchPredictionRequest>>.New
            .With(x => x.LockedUntilUtc, DateTime.UtcNow.AddMinutes(5))
            .With(x => x.DeserializedBody, DefaultRequestBuilder.Build());

        private IServiceBusMessageReceiver<IdentifiedMatchPredictionRequest> messageReceiver;
        private IMessageProcessor<IdentifiedMatchPredictionRequest> messageProcessor;
        private IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private IMatchPredictionRequestResultUploader resultUploader;
        private IMatchPredictionLogger<MatchPredictionRequestLoggingContext> logger;
        private MatchPredictionRequestLoggingContext loggingContext;

        private IMatchPredictionRequestRunner runner;

        [SetUp]
        public void SetUp()
        {
            messageReceiver = Substitute.For<IServiceBusMessageReceiver<IdentifiedMatchPredictionRequest>>();
            messageProcessor = new MessageProcessor<IdentifiedMatchPredictionRequest>(messageReceiver);

            matchPredictionAlgorithm = Substitute.For<IMatchPredictionAlgorithm>();
            resultUploader = Substitute.For<IMatchPredictionRequestResultUploader>();
            logger = Substitute.For<IMatchPredictionLogger<MatchPredictionRequestLoggingContext>>();
            loggingContext = new MatchPredictionRequestLoggingContext();
            var settings = new MatchPredictionRequestsSettings { BatchSize = BatchSize };

            runner = new MatchPredictionRequestRunner(messageProcessor, matchPredictionAlgorithm, resultUploader, logger, loggingContext, settings);
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_RunsMatchPredictionForEachRequest()
        {
            const int requestCount = 2;

            var messageBatch = DefaultMessageBuilder.Build(requestCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);

            await runner.RunMatchPredictionRequestBatch();

            await matchPredictionAlgorithm.Received(requestCount).RunMatchPredictionAlgorithm(Arg.Any<SingleDonorMatchProbabilityInput>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_IgnoresEmptyMessages()
        {
            const int nonEmptyMessageCount = 2;
            const int emptyMessageCount = 3;

            var messageBatch = DefaultMessageBuilder.Build(nonEmptyMessageCount);
            var emptyMessages = DefaultMessageBuilder.With(x => x.DeserializedBody, (IdentifiedMatchPredictionRequest)null).Build(emptyMessageCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch.Concat(emptyMessages));

            await runner.RunMatchPredictionRequestBatch();

            await matchPredictionAlgorithm.Received(nonEmptyMessageCount).RunMatchPredictionAlgorithm(Arg.Any<SingleDonorMatchProbabilityInput>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_UploadsResultsForEachRequest()
        {
            const int requestCount = 4;

            var messageBatch = DefaultMessageBuilder.Build(requestCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ReturnsForAnyArgs(new MatchProbabilityResponse());

            await runner.RunMatchPredictionRequestBatch();

            await resultUploader.Received(requestCount).UploadMatchPredictionRequestResult(DefaultRequestId, Arg.Any<MatchProbabilityResponse>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidRequestInBatch_LogsInvalidRequest()
        {
            const int invalidRequestCount = 1;

            var messageBatch = DefaultMessageBuilder.Build(invalidRequestCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new ValidationException("error"));

            await runner.RunMatchPredictionRequestBatch();

            logger.Received(invalidRequestCount).SendTrace(Arg.Any<string>(), LogLevel.Error, Arg.Any<Dictionary<string, string>>());
        }

        [Test]
        public void RunMatchPredictionRequestBatch_InvalidRequestInBatch_DoesNotThrow()
        {
            var messageBatch = DefaultMessageBuilder.Build(1);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new ValidationException("error"));

            runner.Invoking(async service => await service.RunMatchPredictionRequestBatch()).Should().NotThrow();
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidRequestInBatch_RunsRemainingValidRequests()
        {
            const int invalidDonorId = 123;
            const int validDonorId = 345;

            const int invalidRequestCount = 1;
            const int validRequestCount = 2;

            var invalidRequest = DefaultRequestBuilder.WithDonorOfId(invalidDonorId);
            var invalidRequests = DefaultMessageBuilder.With(x => x.DeserializedBody, invalidRequest).Build(invalidRequestCount);
            var validRequest = DefaultRequestBuilder.WithDonorOfId(validDonorId);
            var validRequests = DefaultMessageBuilder.With(x => x.DeserializedBody, validRequest).Build(validRequestCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(invalidRequests.Concat(validRequests));

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(invalidDonorId)))
                .ThrowsForAnyArgs(new ValidationException("error"));
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(validDonorId)))
                .Returns(new MatchProbabilityResponse());

            await runner.RunMatchPredictionRequestBatch();

            await resultUploader.Received(validRequestCount).UploadMatchPredictionRequestResult(DefaultRequestId, Arg.Any<MatchProbabilityResponse>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidHlaInBatch_LogsInvalidHla()
        {
            const int invalidRequestCount = 1;
            const string locus = "A*";
            const string hlaName = "invalid-hla";

            var messageBatch = DefaultMessageBuilder.Build(invalidRequestCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException(locus, hlaName, "message"));

            await runner.RunMatchPredictionRequestBatch();

            logger.Received(invalidRequestCount).SendTrace(
                Arg.Any<string>(),
                LogLevel.Error, 
                Arg.Is<Dictionary<string, string>>(x => x.ContainsValue(locus) && x.ContainsValue(hlaName)));
        }

        [Test]
        public void RunMatchPredictionRequestBatch_InvalidHlaInBatch_DoesNotThrow()
        {
            var messageBatch = DefaultMessageBuilder.Build(1);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException("A*", "invalid-hla", "message"));

            runner.Invoking(async service => await service.RunMatchPredictionRequestBatch()).Should().NotThrow();
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidHlaInBatch_RunsRemainingValidRequests()
        {
            const int invalidDonorId = 123;
            const int validDonorId = 345;

            const int invalidRequestCount = 1;
            const int validRequestCount = 2;

            var invalidRequest = DefaultRequestBuilder.WithDonorOfId(invalidDonorId);
            var invalidRequests = DefaultMessageBuilder.With(x => x.DeserializedBody, invalidRequest).Build(invalidRequestCount);
            var validRequest = DefaultRequestBuilder.WithDonorOfId(validDonorId);
            var validRequests = DefaultMessageBuilder.With(x => x.DeserializedBody, validRequest).Build(validRequestCount);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(invalidRequests.Concat(validRequests));

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(invalidDonorId)))
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException("A*", "invalid-hla", "message"));
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(validDonorId)))
                .Returns(new MatchProbabilityResponse());

            await runner.RunMatchPredictionRequestBatch();

            await resultUploader.Received(validRequestCount).UploadMatchPredictionRequestResult(DefaultRequestId, Arg.Any<MatchProbabilityResponse>());
        }

        [Test]
        public void RunMatchPredictionRequestBatch_OtherExceptionOccurs_AllowsExceptionToBeThrown()
        {
            var messageBatch = DefaultMessageBuilder.Build(1);
            messageReceiver.ReceiveMessageBatchAsync(default, default).ReturnsForAnyArgs(messageBatch);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new Exception());

            runner.Invoking(async service => await service.RunMatchPredictionRequestBatch()).Should().Throw<Exception>();
        }
    }

    internal static class MatchPredictionRunnerTestsBuilderExtensions
    {
        public static Builder<IdentifiedMatchPredictionRequest> WithDonorOfId(this Builder<IdentifiedMatchPredictionRequest> builder, int donorId)
        {
            return builder.With(x => x.SingleDonorMatchProbabilityInput, new SingleDonorMatchProbabilityInput
            {
                Donor = new DonorInput { DonorId = donorId }
            });
        }
    }
}
