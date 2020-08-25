using Atlas.MatchPrediction.Test.Verification.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class NormalisedPoolMemberBuilder
    {
        public static Builder<NormalisedPoolMember> New =>
        Builder<NormalisedPoolMember>.New
            .With(x => x.HaplotypeFrequency, HaplotypeFrequencyBuilder.Default);
    }
}
