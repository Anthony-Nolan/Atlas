using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Models;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class NormalisedPoolMemberBuilder
{
    public static IPostprocessComposer<NormalisedPoolMember> New =>
        FixtureBuilder.For<NormalisedPoolMember>()
            .With(x => x.HaplotypeFrequency, HaplotypeFrequencyBuilder.Default.Build());
}