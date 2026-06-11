using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.ExternalInterface.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using AutoFixture.Dsl;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services;

[TestFixture]
internal class MatchPredictionRequestDispatcherTests
{
    private static readonly IPostprocessComposer<SingleDonorMatchProbabilityInput> InputMissingDonorInfo = SingleDonorMatchProbabilityInputBuilder.Default
        .With(x => x.Donor, (DonorInput)null);

    private IMessageBatchPublisher<IdentifiedMatchPredictionRequest> requestPublisher;
    private IMatchPredictionRequestDispatcher dispatcher;

    [SetUp]
    public void SetUp()
    {
        requestPublisher = Substitute.For<IMessageBatchPublisher<IdentifiedMatchPredictionRequest>>();
        dispatcher = new MatchPredictionRequestDispatcher(requestPublisher);
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_ReturnsResponseForEachInput()
    {
        const int donorCount = 2;

        var inputs = SingleDonorMatchProbabilityInputBuilder.Valid.Build(donorCount);
        var response = await dispatcher.DispatchMatchPredictionRequestBatch(inputs);

        response.DonorResponses.Count.Should().Be(donorCount);
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_ValidInput_ReturnsMatchPredictionRequestId()
    {
        var inputs = SingleDonorMatchProbabilityInputBuilder.Valid.Build(1);
        var response = await dispatcher.DispatchMatchPredictionRequestBatch(inputs);

        response.DonorResponses.Single().MatchPredictionRequestId.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_InvalidInput_DoesNotThrow()
    {
        await dispatcher.Invoking(service => service.DispatchMatchPredictionRequestBatch(InputMissingDonorInfo.Build(1)))
            .Should().NotThrowAsync();
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_InvalidInput_ReturnsValidationErrors()
    {
        var response = await dispatcher.DispatchMatchPredictionRequestBatch(InputMissingDonorInfo.Build(1));

        response.DonorResponses.Single().ValidationErrors.Should().NotBeEmpty();
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_InvalidInput_DoesNotReturnRequestId()
    {
        var response = await dispatcher.DispatchMatchPredictionRequestBatch(InputMissingDonorInfo.Build(1));

        response.DonorResponses.Single().MatchPredictionRequestId.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_MixedInput_OnlyReturnsMatchPredictionRequestIdForValidInput()
    {
        var validInput = SingleDonorMatchProbabilityInputBuilder.Valid.Build(1);
        var response = await dispatcher.DispatchMatchPredictionRequestBatch(validInput.Concat(InputMissingDonorInfo.Build(1)));

        response.DonorResponses.First().MatchPredictionRequestId.Should().NotBeNullOrEmpty();
        response.DonorResponses.Last().MatchPredictionRequestId.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_MixedInput_OnlyReturnsValidationErrorsForInvalidInput()
    {
        var validInput = SingleDonorMatchProbabilityInputBuilder.Valid.Build(1);
        var response = await dispatcher.DispatchMatchPredictionRequestBatch(validInput.Concat(InputMissingDonorInfo.Build(1)));

        response.DonorResponses.First().ValidationErrors.Should().BeNullOrEmpty();
        response.DonorResponses.Last().ValidationErrors.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_MixedInput_OnlyBatchPublishesValidRequests()
    {
        const int validDonorCount = 5;
        const int invalidDonorCount = 3;
        const int validDonorId = 567;

        var validInput = SingleDonorMatchProbabilityInputBuilder.Valid.WithDonorId(validDonorId).Build(validDonorCount);
        await dispatcher.DispatchMatchPredictionRequestBatch(validInput.Concat(InputMissingDonorInfo.Build(invalidDonorCount)));

        await requestPublisher.Received(1).BatchPublish(Arg.Is<IEnumerable<IdentifiedMatchPredictionRequest>>(x =>
            x.Count() == validDonorCount &&
            x.SelectMany(r => r.SingleDonorMatchProbabilityInput.Donor.DonorIds).Distinct().Single() == validDonorId));
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_MultipleDonorIdsInOneInput_OnlyReturnsResponseForFirstDonorId()
    {
        const int firstDonorId = 1;

        var input = SingleDonorMatchProbabilityInputBuilder.Default.With(x => x.Donor, new DonorInput
        {
            DonorIds = new List<int> { firstDonorId, 2, 3 }
        }).Build(1);

        var response = await dispatcher.DispatchMatchPredictionRequestBatch(input);

        response.DonorResponses.Single().DonorId.Should().Be(firstDonorId);
    }

    [Test]
    public async Task DispatchMatchPredictionRequestBatch_OtherExceptionOccurs_ThrowsException()
    {
        requestPublisher.BatchPublish(default).ThrowsForAnyArgs(new Exception("error"));

        var input = SingleDonorMatchProbabilityInputBuilder.Valid.Build(1);

        await dispatcher.Invoking(service => service.DispatchMatchPredictionRequestBatch(input)).Should().ThrowAsync<Exception>();
    }
}