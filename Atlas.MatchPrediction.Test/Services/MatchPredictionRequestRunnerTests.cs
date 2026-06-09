using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.ExternalInterface.ResultsUpload;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using FluentAssertions;
using FluentValidation;
using LochNessBuilder;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
        private IMessageBatchPublisher<MatchPredictionResultLocation> locationPublisher;
        private IAtlasLogger batchLogger;
        private IMatchPredictionLogger<MatchPredictionRequestLoggingContext> logger;
        private MatchPredictionRequestsSettings settings;
        private ServiceProvider serviceProvider;

        private IMatchPredictionRequestRunner runner;

        [SetUp]
        public void SetUp()
        {
            matchPredictionAlgorithm = Substitute.For<IMatchPredictionAlgorithm>();
            resultUploader = Substitute.For<IMatchPredictionRequestResultUploader>();
            locationPublisher = Substitute.For<IMessageBatchPublisher<MatchPredictionResultLocation>>();
            batchLogger = Substitute.For<IAtlasLogger>();
            logger = Substitute.For<IMatchPredictionLogger<MatchPredictionRequestLoggingContext>>();
            settings = new MatchPredictionRequestsSettings { MaxParallelism = 4 };

            serviceProvider = new ServiceCollection()
                .AddScoped(_ => matchPredictionAlgorithm)
                .AddScoped(_ => resultUploader)
                .AddScoped(_ => locationPublisher)
                .AddScoped<IAtlasLogger>(_ => batchLogger)
                .AddScoped(_ => logger)
                .AddSingleton(_ => settings)
                .AddScoped<MatchPredictionRequestLoggingContext>()
                .AddScoped<IMatchPredictionRequestRunner, MatchPredictionRequestRunner>()
                .BuildServiceProvider();

            runner = serviceProvider.GetRequiredService<IMatchPredictionRequestRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            serviceProvider?.Dispose();
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
        public async Task RunMatchPredictionRequestBatch_BatchPublishesResultFileLocations()
        {
            const int requestCount = 3;

            var requestBatch = DefaultRequestBuilder.Build(requestCount);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ReturnsForAnyArgs(new MatchProbabilityResponse());
            resultUploader.UploadMatchPredictionRequestResult(default, default).ReturnsForAnyArgs(new MatchPredictionResultLocation());

            await runner.RunMatchPredictionRequestBatch(requestBatch);

            await locationPublisher.Received(1).BatchPublish(Arg.Is<IEnumerable<MatchPredictionResultLocation>>(x => x.Count() == requestCount));
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
        public async Task RunMatchPredictionRequestBatch_InvalidRequestInBatch_DoesNotThrow()
        {
            var requestBatch = DefaultRequestBuilder.Build(1);
            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new ValidationException("error"));

            await runner.Invoking(service => service.RunMatchPredictionRequestBatch(requestBatch)).Should().NotThrowAsync();
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
        public async Task RunMatchPredictionRequestBatch_InvalidHlaInBatch_DoesNotThrow()
        {
            var requestBatch = DefaultRequestBuilder.Build(1);

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new HlaMetadataDictionaryException("A*", "invalid-hla", "message"));

            await runner.Invoking(service => service.RunMatchPredictionRequestBatch(requestBatch)).Should().NotThrowAsync();
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
        public async Task RunMatchPredictionRequestBatch_OtherExceptionOccurs_AllowsExceptionToBeThrown()
        {
            var requestBatch = DefaultRequestBuilder.Build(1);

            matchPredictionAlgorithm.RunMatchPredictionAlgorithm(default).ThrowsForAnyArgs(new Exception());

            await runner.Invoking(service => service.RunMatchPredictionRequestBatch(requestBatch)).Should().ThrowAsync<Exception>();
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
