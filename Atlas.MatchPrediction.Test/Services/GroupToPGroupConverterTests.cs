using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Services.HlaConversion;
using AutoFixture;
using AutoFixture.Dsl;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services;

/// <summary>
/// The two <see cref="HlaConverterBase"/> implementations that turn a pooled haplotype's group name into a P group.
///
/// <para>
/// The outcome to pin is the one a bare value cannot express: a group of null-expressing alleles <b>is</b> in the data
/// and has no P group. Reporting that as a failed conversion would log an error, retry the lookup at the other
/// nomenclature version, and hand the caller nothing where it expects a null.
/// </para>
/// </summary>
[TestFixture]
internal class GroupToPGroupConverterTests
{
    private const Locus DefaultLocus = Locus.A;

    private IHlaMetadataDictionary hmd;
    private IMatchPredictionLogger<MatchProbabilityLoggingContext> logger;
    private IPostprocessComposer<HlaConverterInput> inputBuilder;
    private Fixture fixture;

    private IHlaConverter gGroupConverter;
    private IHlaConverter smallGGroupConverter;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        logger = Substitute.For<IMatchPredictionLogger<MatchProbabilityLoggingContext>>();
        hmd = Substitute.For<IHlaMetadataDictionary>();

        gGroupConverter = new GGroupToPGroupConverter(logger);
        smallGGroupConverter = new SmallGGroupToPGroupConverter(logger);

        // No matching-algorithm dictionary, so a failed conversion is not retried at a second nomenclature version:
        // these tests are about what one lookup reports. HlaConverterTests covers the retry.
        inputBuilder = FixtureBuilder.For<HlaConverterInput>()
            .With(x => x.HfSetHmd, hmd)
            .With(x => x.MatchingAlgorithmHmd, (IHlaMetadataDictionary)null);
    }

    [Test]
    public async Task GGroupToPGroup_WhenTheGroupHasAPGroup_ReturnsIt()
    {
        var gGroup = fixture.Create<string>();
        var pGroup = fixture.Create<string>();
        hmd.TryConvertGGroupToPGroup(DefaultLocus, gGroup).Returns((true, pGroup));

        var result = await gGroupConverter.ConvertHlaWithLoggingAndRetryOnFailure(inputBuilder.Build(), DefaultLocus, gGroup);

        result.Should().Equal(pGroup);
        ShouldNotHaveLoggedAFailure();
    }

    [Test]
    public async Task GGroupToPGroup_WhenTheGroupIsFoundAndHasNoPGroup_ReturnsASingleNullAndLogsNothing()
    {
        var gGroup = fixture.Create<string>();
        hmd.TryConvertGGroupToPGroup(DefaultLocus, gGroup).Returns((true, (string) null));

        var result = await gGroupConverter.ConvertHlaWithLoggingAndRetryOnFailure(inputBuilder.Build(), DefaultLocus, gGroup);

        result.Should().ContainSingle().Which.Should().BeNull();
        ShouldNotHaveLoggedAFailure();
    }

    [Test]
    public async Task GGroupToPGroup_WhenTheGroupHasNoData_ReturnsNothingAndLogsTheFailure()
    {
        var gGroup = fixture.Create<string>();
        hmd.TryConvertGGroupToPGroup(DefaultLocus, gGroup).Returns((false, (string) null));

        var result = await gGroupConverter.ConvertHlaWithLoggingAndRetryOnFailure(inputBuilder.Build(), DefaultLocus, gGroup);

        result.Should().BeEmpty();
        ShouldHaveLoggedOneFailure();
    }

    [Test]
    public async Task SmallGGroupToPGroup_WhenTheGroupHasAPGroup_ReturnsIt()
    {
        var smallGGroup = fixture.Create<string>();
        var pGroup = fixture.Create<string>();
        hmd.TryConvertSmallGGroupToPGroup(DefaultLocus, smallGGroup).Returns((true, pGroup));

        var result = await smallGGroupConverter.ConvertHlaWithLoggingAndRetryOnFailure(inputBuilder.Build(), DefaultLocus, smallGGroup);

        result.Should().Equal(pGroup);
        ShouldNotHaveLoggedAFailure();
    }

    [Test]
    public async Task SmallGGroupToPGroup_WhenTheGroupIsFoundAndHasNoPGroup_ReturnsASingleNullAndLogsNothing()
    {
        var smallGGroup = fixture.Create<string>();
        hmd.TryConvertSmallGGroupToPGroup(DefaultLocus, smallGGroup).Returns((true, (string) null));

        var result = await smallGGroupConverter.ConvertHlaWithLoggingAndRetryOnFailure(inputBuilder.Build(), DefaultLocus, smallGGroup);

        result.Should().ContainSingle().Which.Should().BeNull();
        ShouldNotHaveLoggedAFailure();
    }

    [Test]
    public async Task SmallGGroupToPGroup_WhenTheGroupHasNoData_ReturnsNothingAndLogsTheFailure()
    {
        var smallGGroup = fixture.Create<string>();
        hmd.TryConvertSmallGGroupToPGroup(DefaultLocus, smallGGroup).Returns((false, (string) null));

        var result = await smallGGroupConverter.ConvertHlaWithLoggingAndRetryOnFailure(inputBuilder.Build(), DefaultLocus, smallGGroup);

        result.Should().BeEmpty();
        ShouldHaveLoggedOneFailure();
    }

    private void ShouldHaveLoggedOneFailure() => logger.Received(1).SendEvent(
        Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());

    private void ShouldNotHaveLoggedAFailure() => logger.DidNotReceiveWithAnyArgs().SendEvent(
        Arg.Any<string>(), Arg.Any<LogLevel>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<Dictionary<string, double>>());
}
