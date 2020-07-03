using Atlas.MatchPrediction.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    public static class IndividualPopulationDataBuilder
    {
        public static Builder<IndividualPopulationData> New => Builder<IndividualPopulationData>.New;

        public static Builder<IndividualPopulationData> ForRegistry(this Builder<IndividualPopulationData> builder, string registryCode)
        {
            return builder.With(d => d.RegistryCode, registryCode);
        }

        public static Builder<IndividualPopulationData> ForEthnicity(this Builder<IndividualPopulationData> builder, string ethnicityCode)
        {
            return builder.With(d => d.EthnicityCode, ethnicityCode);
        }
    }
}