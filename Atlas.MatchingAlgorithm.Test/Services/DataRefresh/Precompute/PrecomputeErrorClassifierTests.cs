using System;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using AutoFixture;
using AwesomeAssertions;
using Azure;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.DataRefresh.Precompute;

/// <remarks>
/// A <c>SqlException</c> has no public constructor, so its branch is not tested here.
/// </remarks>
[TestFixture]
public class PrecomputeErrorClassifierTests
{
    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public void Classify_AnHlaMetadataDictionaryException_IsKnownPermanent()
    {
        // The dictionary throws it only to say that a name has no data: a bad typing, which no retry fixes.
        var exception = new HlaMetadataDictionaryException(fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>());

        PrecomputeErrorClassifier.Classify(exception).Should().Be(PrecomputeErrorKind.KnownPermanent);
    }

    [TestCase(0)]
    [TestCase(408)]
    [TestCase(429)]
    [TestCase(500)]
    [TestCase(503)]
    public void Classify_AStorageRequestThatARetryCanFix_IsKnownTemporary(int status)
    {
        var exception = new RequestFailedException(status, fixture.Create<string>());

        PrecomputeErrorClassifier.Classify(exception).Should().Be(PrecomputeErrorKind.KnownTemporary);
    }

    [TestCase(400)]
    [TestCase(403)]
    [TestCase(404)]
    public void Classify_AStorageRequestThatARetryCannotFix_IsUnknown(int status)
    {
        var exception = new RequestFailedException(status, fixture.Create<string>());

        PrecomputeErrorClassifier.Classify(exception).Should().Be(PrecomputeErrorKind.Unknown);
    }

    [Test]
    public void Classify_ATimeout_IsKnownTemporary()
    {
        PrecomputeErrorClassifier.Classify(new TimeoutException()).Should().Be(PrecomputeErrorKind.KnownTemporary);
    }

    [Test]
    public void Classify_AnHttpError_IsKnownTemporary()
    {
        PrecomputeErrorClassifier.Classify(new HttpRequestException()).Should().Be(PrecomputeErrorKind.KnownTemporary);
    }

    [Test]
    public void Classify_AWrappedTimeout_IsKnownTemporary()
    {
        // An HTTP timeout arrives as a cancellation around the timeout.
        var exception = new TaskCanceledException(fixture.Create<string>(), new TimeoutException());

        PrecomputeErrorClassifier.Classify(exception).Should().Be(PrecomputeErrorKind.KnownTemporary);
    }

    [Test]
    public void Classify_AnAggregateWithATemporaryError_IsKnownTemporary()
    {
        var exception = new AggregateException(new InvalidOperationException(), new TimeoutException());

        PrecomputeErrorClassifier.Classify(exception).Should().Be(PrecomputeErrorKind.KnownTemporary);
    }

    [Test]
    public void Classify_AnHlaMetadataDictionaryExceptionAroundATemporaryError_IsKnownTemporary()
    {
        // Temporary wins: a retry costs one attempt of a batch, and a permanent failure costs its donors their rows.
        var exception = new HlaMetadataDictionaryException(
            fixture.Create<string>(), fixture.Create<string>(), fixture.Create<string>(), new TimeoutException());

        PrecomputeErrorClassifier.Classify(exception).Should().Be(PrecomputeErrorKind.KnownTemporary);
    }

    [Test]
    public void Classify_AnyOtherError_IsUnknown()
    {
        PrecomputeErrorClassifier.Classify(new InvalidOperationException(fixture.Create<string>())).Should().Be(PrecomputeErrorKind.Unknown);
    }
}
