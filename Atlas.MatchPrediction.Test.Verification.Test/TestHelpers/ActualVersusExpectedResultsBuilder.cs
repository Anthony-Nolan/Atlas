using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Models;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class ActualVersusExpectedResultBuilder
{
    public static IPostprocessComposer<ActualVersusExpectedResult> New => FixtureBuilder.For<ActualVersusExpectedResult>();

    public static IPostprocessComposer<ActualVersusExpectedResult> WithProbabilityAndCounts(this IPostprocessComposer<ActualVersusExpectedResult> builder, int probability, int pdpCount)
    {
        return builder
            .With(x => x.Probability, probability)
            .With(x => x.ActuallyMatchedPdpCount, pdpCount)
            .With(x => x.TotalPdpCount, pdpCount);
    }
}