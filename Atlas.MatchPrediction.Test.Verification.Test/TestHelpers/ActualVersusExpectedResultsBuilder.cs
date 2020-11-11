using Atlas.MatchPrediction.Test.Verification.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class ActualVersusExpectedResultBuilder
    {
        public static Builder<ActualVersusExpectedResult> New => Builder<ActualVersusExpectedResult>.New;

        public static Builder<ActualVersusExpectedResult> WithProbabilityAndCounts(this Builder<ActualVersusExpectedResult> builder, int probability, int pdpCount)
        {
            return builder
                .With(x => x.Probability, probability)
                .With(x => x.ActuallyMatchedPdpCount, pdpCount)
                .With(x => x.TotalPdpCount, pdpCount);
        }
    }
}
