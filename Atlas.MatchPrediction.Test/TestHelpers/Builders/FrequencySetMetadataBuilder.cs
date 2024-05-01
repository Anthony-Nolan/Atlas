using Atlas.Common.Public.Models.MatchPrediction;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    public static class FrequencySetMetadataBuilder
    {
        public static Builder<FrequencySetMetadata> New => Builder<FrequencySetMetadata>.New;

        public static Builder<FrequencySetMetadata> ForRegistry(this Builder<FrequencySetMetadata> builder, string registryCode)
        {
            return builder.With(d => d.RegistryCode, registryCode);
        }

        public static Builder<FrequencySetMetadata> ForEthnicity(this Builder<FrequencySetMetadata> builder, string ethnicityCode)
        {
            return builder.With(d => d.EthnicityCode, ethnicityCode);
        }
    }
}