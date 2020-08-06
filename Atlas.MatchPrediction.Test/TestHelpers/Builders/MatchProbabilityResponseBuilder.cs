using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.MatchProbabilityResponse>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class MatchProbabilityResponseBuilder
    {
        public static Builder New => Builder.New;

        public static Builder WithAllProbabilityValuesSetTo(this Builder builder, decimal value)
        {
            return builder.WithAllProbabilityValuesSetTo((decimal?) value);
        }

        public static Builder WithAllProbabilityValuesNull(this Builder builder)
        {
            return builder.WithAllProbabilityValuesSetTo(null);
        }

        private static Builder WithAllProbabilityValuesSetTo(this Builder builder, decimal? value)
        {
            var matchProbabilities = new MatchProbabilities(value != null ? new Probability(value.Value) : null);

            return builder
                .With(r => r.MatchProbabilities, matchProbabilities)
                .With(r => r.MatchProbabilitiesPerLocus, new LociInfo<MatchProbabilityLocusInfo>(new MatchProbabilityLocusInfo(matchProbabilities)));
        }
    }
}