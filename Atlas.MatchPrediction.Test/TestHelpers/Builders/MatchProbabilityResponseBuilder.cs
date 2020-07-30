using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;
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
            return builder
                .With(r => r.ZeroMismatchProbability, new Probability(value))
                .With(r => r.OneMismatchProbability, new Probability(value))
                .With(r => r.TwoMismatchProbability, new Probability(value))
                .With(r => r.ZeroMismatchProbabilityPerLocus, new LociInfo<Probability>(new Probability(value)))
                .With(r => r.OneMismatchProbabilityPerLocus, new LociInfo<Probability>(new Probability(value)))
                .With(r => r.TwoMismatchProbabilityPerLocus, new LociInfo<Probability>(new Probability(value)));
        }
        
        public static Builder WithAllProbabilityValuesNull(this Builder builder)
        {
            return builder
                .With(r => r.ZeroMismatchProbability, (Probability) null)
                .With(r => r.OneMismatchProbability, (Probability) null)
                .With(r => r.TwoMismatchProbability, (Probability) null)
                .With(r => r.ZeroMismatchProbabilityPerLocus, new LociInfo<Probability>(null))
                .With(r => r.OneMismatchProbabilityPerLocus, new LociInfo<Probability>(null))
                .With(r => r.TwoMismatchProbabilityPerLocus, new LociInfo<Probability>(null));
        }
    }
}