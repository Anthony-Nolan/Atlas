using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.Client.Models.Search.Results.MatchPrediction.MatchProbabilities>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders;

internal static class MatchProbabilitiesBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.Client.Models.Search.Results.MatchPrediction.MatchProbabilities>();

    public static Composer WithAllProbabilityValuesSetTo(this Composer builder, decimal value)
    {
        return builder.WithProbabilityValuesSetTo(value, value, value);
    }

    public static Composer WithProbabilityValuesSetTo(
        this Composer builder,
        decimal zeroMismatchValue,
        decimal oneMismatchValue,
        decimal twoMismatchValue)
    {
        return builder.With(r => r.ZeroMismatchProbability, new Probability(zeroMismatchValue))
            .With(r => r.OneMismatchProbability, new Probability(oneMismatchValue))
            .With(r => r.TwoMismatchProbability, new Probability(twoMismatchValue));
    }

    public static Composer WithZeroMismatchProbability(this Composer builder, decimal zeroMismatchValue)
    {
        return builder.With(r => r.ZeroMismatchProbability, new Probability(zeroMismatchValue));
    }
}