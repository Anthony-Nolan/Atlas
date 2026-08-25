using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Services.HlaConversion;
using AutoFixture.Dsl;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services;

/// <summary>
/// Covers base logic of <see cref="HlaConverterBase"/> via one concrete implementation, <see cref="HlaToTargetCategoryConverter"/>.
/// </summary>
[TestFixture]
internal class HlaConverterTests
{
    private const Locus DefaultLocus = Locus.A;
    private const string HlaName = "hla";

    private const string HfSetHlaVersion = "hf-set-hla-version";
    private const string MatchingAlgorithmHlaVersion = "matching-hla-version";

    private static readonly IReadOnlyCollection<string> HfSetHmdResult = new []{"hf-set-hmd-result"};
    private static readonly IReadOnlyCollection<string> MatchingHmdResult = new[] { "matching-hmd-result" };

    private IHlaMetadataDictionary hfSetHmd;
    private IHlaMetadataDictionary matchingHmd;
    private IPostprocessComposer<HlaConverterInput> inputBuilder;

    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
    private IHlaConverter converter;

    [SetUp]
    public void SetUp()
    {
        logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
        converter = new HlaToTargetCategoryConverter(logger);

        hfSetHmd = Substitute.For<IHlaMetadataDictionary>();
        hfSetHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);
        hfSetHmd.TryConvertHla(default, default, default).ReturnsForAnyArgs((true, HfSetHmdResult));

        matchingHmd = Substitute.For<IHlaMetadataDictionary>();
        matchingHmd.HlaNomenclatureVersion.Returns(MatchingAlgorithmHlaVersion);
        matchingHmd.TryConvertHla(default, default, default).ReturnsForAnyArgs((true, MatchingHmdResult));

        inputBuilder = FixtureBuilder.For<HlaConverterInput>();
    }

    /// <summary>
    /// A name with no data is <c>(false, null)</c>, not an exception. Every "arrange lookup failure"
    /// below used to be <c>ThrowsForAnyArgs(new HlaMetadataDictionaryException(...))</c>; the assertions are unchanged.
    /// </summary>
    private static void ArrangeLookupFailure(IHlaMetadataDictionary hmd) =>
        hmd.TryConvertHla(default, default, default).ReturnsForAnyArgs((false, (IReadOnlyCollection<string>)null));

    #region DoesNotTryLookupUsingMatchingAlgorithmHmd tests

    [Test]
    public void HlaConverterInput_NoMatchingAlgorithmHmd_DoesNotRetryLookupUsingMatchingAlgorithmHmd()
    {
        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .Build();

        input.DoNotRetryLookupUsingMatchingAlgorithmHmd.Should().BeTrue();
    }

    [Test]
    public void HlaConverterInput_HasMatchingAlgorithmHmdOfSameHlaVersion_DoesNotRetryLookupUsingMatchingAlgorithmHmd()
    {
        matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .Build();

        input.DoNotRetryLookupUsingMatchingAlgorithmHmd.Should().BeTrue();
    }

    [Test]
    public void HlaConverterInput_HasMatchingAlgorithmHmdOfDifferentHlaVersion_DoesRetryLookupUsingMatchingAlgorithmHmd()
    {
        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .Build();

        input.DoNotRetryLookupUsingMatchingAlgorithmHmd.Should().BeFalse();
    }

    #endregion

    #region ConvertHlaWithLoggingAndRetryOnFailure tests

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_ConvertsHla([Values] TargetHlaCategory target)
    {
        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        await hfSetHmd.Received().TryConvertHla(DefaultLocus, HlaName, target);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_ReturnsResults(
        [Values] TargetHlaCategory target)
    {
        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        results.Should().BeEquivalentTo(HfSetHmdResult);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_DoesNotLogFailure(
        [Values] TargetHlaCategory target)
    {
        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        logger.DidNotReceiveWithAnyArgs().SendEvent(Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_AndRetryEnabled_DoesNotRetryLookup(
        [Values] TargetHlaCategory target)
    {
        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        await matchingHmd.DidNotReceiveWithAnyArgs().TryConvertHla(default, default, default);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupSucceeds_AndRetryDisabled_DoesNotRetryLookup(
        [Values] TargetHlaCategory target)
    {
        // disables retry
        matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        await matchingHmd.DidNotReceiveWithAnyArgs().TryConvertHla(default, default, default);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_LogsFailure(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        logger.Received(1).SendEvent(Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryEnabled_RetriesLookupUsingMatchingHmd(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        await matchingHmd.Received().TryConvertHla(DefaultLocus, HlaName, target);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryEnabled_ReturnsResults(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        results.Should().BeEquivalentTo(MatchingHmdResult);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryDisabled_DoesNotRetryLookup(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        // disables retry
        matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        await matchingHmd.DidNotReceiveWithAnyArgs().TryConvertHla(default, default, default);
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_FirstLookupFails_AndRetryDisabled_ReturnsEmptyCollection(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        // disables retry
        matchingHmd.HlaNomenclatureVersion.Returns(HfSetHlaVersion);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        results.Should().BeEmpty();
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_SecondLookupFails_LogsFirstAndSecondFailure(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        ArrangeLookupFailure(matchingHmd);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        logger.Received(2).SendEvent(Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_SecondLookupFails_ReturnsEmptyCollection(
        [Values] TargetHlaCategory target)
    {
        ArrangeLookupFailure(hfSetHmd);

        ArrangeLookupFailure(matchingHmd);

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        var results = await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        results.Should().BeEmpty();
    }

    [Test]
    public async Task ConvertHlaWithLoggingAndRetryOnFailure_WhenTheLookupFaults_PropagatesInsteadOfReportingNoData(
        [Values] TargetHlaCategory target)
    {
        // A failed storage request is not a missing name, and this is the case that used to be
        // indistinguishable: both arrived as HlaMetadataDictionaryException, both were swallowed into an empty result,
        // so a transient blip silently read as "this HLA does not exist" and match prediction ran on an incomplete
        // expansion with nothing logged as an error. Only a name with no data returns (false, null) now; anything else
        // escapes, and the caller can tell them apart for the first time.
        hfSetHmd.TryConvertHla(default, default, default)
            .ThrowsForAnyArgs(new TimeoutException("storage is having a moment"));

        var input = inputBuilder
            .With(x => x.HfSetHmd, hfSetHmd)
            .With(x => x.MatchingAlgorithmHmd, matchingHmd)
            .With(x => x.TargetHlaCategory, target)
            .Build();

        var convert = async () => await converter.ConvertHlaWithLoggingAndRetryOnFailure(input, DefaultLocus, HlaName);

        await convert.Should().ThrowAsync<TimeoutException>();

        // And it is not reported as a conversion failure, because it is not one.
        logger.DidNotReceiveWithAnyArgs().SendEvent(
            Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
    }

    #endregion
}