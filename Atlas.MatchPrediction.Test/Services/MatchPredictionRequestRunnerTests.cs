using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using FluentValidation;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Services
{
    [TestFixture]
    internal class MatchPredictionRequestRunnerTests
    {
        private const string DefaultRequestId = "request-id";
        private static readonly Builder<IdentifiedMatchPredictionRequest> DefaultRequestBuilder = Builder<IdentifiedMatchPredictionRequest>.New
            .With(x => x.Id, DefaultRequestId)
            .With(x => x.SingleDonorMatchProbabilityInput, new SingleDonorMatchProbabilityInput());

        private IMatchPredictionAlgorithm matchPredictionAlgorithm;
        private IMatchPredictionRequestResultUploader resultUploader;
        private IMatchPredictionLogger<MatchPredictionRequestLoggingContext> logger;
        private MatchPredictionRequestLoggingContext loggingContext;

        private IMatchPredictionRequestRunner runner;

        [SetUp]
        public void SetUp()
        {
            matchPredictionAlgorithm = Substitute.For<IMatchPredictionAlgorithm>();
            resultUploader = Substitute.For<IMatchPredictionRequestResultUploader>();
            logger = Substitute.For<IMatchPredictionLogger<MatchPredictionRequestLoggingContext>>();
            loggingContext = new MatchPredictionRequestLoggingContext();

            runner = new MatchPredictionRequestRunner(matchPredictionAlgorithm, resultUploader, logger, loggingContext);
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_RunsMatchPredictionForEachRequest()
        {
            const int requestCount = 2;

            var requestBatch = DefaultRequestBuilder.Build(requestCount);
            await runner.RunMatchPredictionRequestBatch(requestBatch);

            await matchPredictionAlgorithm.Received(requestCount).RunMatchPredictionAlgorithm(Arg.Any<SingleDonorMatchProbabilityInput>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_IgnoresNullRequests()
        {
            const int notNullMessageCount = 2;

            var requestBatch = new List<IdentifiedMatchPredictionRequest>(DefaultRequestBuilder.Build(notNullMessageCount))
            {
                null, null
            };
            await runner.RunMatchPredictionRequestBatch(requestBatch);

            await matchPredictionAlgorithm.Received(notNullMessageCount).RunMatchPredictionAlgorithm(Arg.Any<SingleDonorMatchProbabilityInput>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_UploadsResultsForEachRequest()
        {
            const int requestCount = 4;

            var requestBatch = DefaultRequestBuilder.Build(requestCount);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ReturnsForAnyArgs(new MatchProbabilityResponse());

            await runner.RunMatchPredictionRequestBatch(requestBatch);

            await resultUploader.Received(requestCount).UploadMatchPredictionRequestResult(DefaultRequestId, Arg.Any<MatchProbabilityResponse>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidRequestInBatch_LogsInvalidRequest()
        {
            const int invalidRequestCount = 1;

            var requestBatch = DefaultRequestBuilder.Build(invalidRequestCount);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new ValidationException("error"));

            await runner.RunMatchPredictionRequestBatch(requestBatch);

            logger.Received(invalidRequestCount).SendTrace(Arg.Any<string>(), LogLevel.Error, Arg.Any<Dictionary<string, string>>());
        }

        [Test]
        public void RunMatchPredictionRequestBatch_InvalidRequestInBatch_DoesNotThrow()
        {
            var requestBatch = DefaultRequestBuilder.Build(1);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new ValidationException("error"));

            runner.Invoking(async service => await service.RunMatchPredictionRequestBatch(requestBatch)).Should().NotThrow();
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidRequestInBatch_RunsRemainingValidRequests()
        {
            const int invalidDonorId = 123;
            const int validDonorId = 345;
            const int invalidRequestCount = 1;
            const int validRequestCount = 2;

            var invalidRequests = DefaultRequestBuilder.WithDonorOfId(invalidDonorId).Build(invalidRequestCount);
            var validRequests = DefaultRequestBuilder.WithDonorOfId(validDonorId).Build(validRequestCount);
            var requestBatch = invalidRequests.Concat(validRequests);

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(invalidDonorId)))
                .ThrowsForAnyArgs(new ValidationException("error"));
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(validDonorId)))
                .Returns(new MatchProbabilityResponse());

            await runner.RunMatchPredictionRequestBatch(requestBatch);

            await resultUploader.Received(validRequestCount).UploadMatchPredictionRequestResult(DefaultRequestId, Arg.Any<MatchProbabilityResponse>());
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidHlaInBatch_LogsInvalidHla()
        {
            const int invalidRequestCount = 1;
            const string locus = "A*";
            const string hlaName = "invalid-hla";

            var requestBatch = DefaultRequestBuilder.Build(invalidRequestCount);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException(locus, hlaName, "message"));

            await runner.RunMatchPredictionRequestBatch(requestBatch);

            logger.Received(invalidRequestCount).SendTrace(
                Arg.Any<string>(),
                LogLevel.Error, 
                Arg.Is<Dictionary<string, string>>(x => x.ContainsValue(locus) && x.ContainsValue(hlaName)));
        }

        [Test]
        public void RunMatchPredictionRequestBatch_InvalidHlaInBatch_DoesNotThrow()
        {
            var requestBatch = DefaultRequestBuilder.Build(1);

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException("A*", "invalid-hla", "message"));

            runner.Invoking(async service => await service.RunMatchPredictionRequestBatch(requestBatch)).Should().NotThrow();
        }

        [Test]
        public async Task RunMatchPredictionRequestBatch_InvalidHlaInBatch_RunsRemainingValidRequests()
        {
            const int invalidDonorId = 123;
            const int validDonorId = 345;
            const int invalidRequestCount = 1;
            const int validRequestCount = 2;

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(invalidDonorId)))
                .ThrowsForAnyArgs(new HlaMetadataDictionaryException("A*", "invalid-hla", "message"));
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(Arg.Is<SingleDonorMatchProbabilityInput>(x => x.Donor.DonorIds.Contains(validDonorId)))
                .Returns(new MatchProbabilityResponse());

            var invalidRequest = DefaultRequestBuilder.WithDonorOfId(invalidDonorId).Build(invalidRequestCount);
            var validRequest = DefaultRequestBuilder.WithDonorOfId(validDonorId).Build(validRequestCount);
            var requestBatch = invalidRequest.Concat(validRequest);

            await runner.RunMatchPredictionRequestBatch(requestBatch);

            await resultUploader.Received(validRequestCount).UploadMatchPredictionRequestResult(DefaultRequestId, Arg.Any<MatchProbabilityResponse>());
        }

        [Test]
        public void RunMatchPredictionRequestBatch_OtherExceptionOccurs_AllowsExceptionToBeThrown()
        {
            var requestBatch = DefaultRequestBuilder.Build(1);

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new Exception());

            runner.Invoking(async service => await service.RunMatchPredictionRequestBatch(requestBatch)).Should().Throw<Exception>();
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
