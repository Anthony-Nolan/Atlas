using Atlas.Common.Utils.Models;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.Client.Models.Search.Results.MatchPrediction.MatchProbabilities>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class MatchProbabilitiesBuilder
    {
        public static Builder New => Builder.New;

        public static Builder WithAllProbabilityValuesSetTo(this Builder builder, decimal value)
        {
            return builder.WithProbabilityValuesSetTo(value, value, value);
        }

        public static Builder WithProbabilityValuesSetTo(
            this Builder builder,
            decimal zeroMismatchValue,
            decimal oneMismatchValue,
            decimal twoMismatchValue)
        {
            return builder.With(r => r.ZeroMismatchProbability, new Probability(zeroMismatchValue))
                .With(r => r.OneMismatchProbability, new Probability(oneMismatchValue))
                .With(r => r.TwoMismatchProbability, new Probability(twoMismatchValue));
        }
    }
}