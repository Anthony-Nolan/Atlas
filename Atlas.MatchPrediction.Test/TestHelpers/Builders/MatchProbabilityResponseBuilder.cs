using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.Client.Models.Search.Results.MatchPrediction.MatchProbabilityResponse>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders;

internal static class MatchProbabilityResponseBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.Client.Models.Search.Results.MatchPrediction.MatchProbabilityResponse>();

    public static Composer WithAllProbabilityValuesSetTo(this Composer builder, decimal value)
    {
        return builder.WithAllProbabilityValuesSetTo((decimal?) value);
    }

    public static Composer WithAllProbabilityValuesNull(this Composer builder)
    {
        return builder.WithAllProbabilityValuesSetTo(null);
    }

    private static Composer WithAllProbabilityValuesSetTo(this Composer builder, decimal? value)
    {
        var matchProbabilities = new MatchProbabilities(value != null ? new Probability(value.Value) : null);

        return builder
            .With(r => r.MatchProbabilities, matchProbabilities)
            .With(r => r.MatchProbabilitiesPerLocus,
                new LociInfo<MatchProbabilityPerLocusResponse>(new MatchProbabilityPerLocusResponse(matchProbabilities)));
    }
}