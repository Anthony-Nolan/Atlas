using Atlas.MatchPrediction.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class HaplotypeFrequencySetMetadataBuilder
    {
        public static Builder<HaplotypeFrequencySetMetadata> Default =>
        Builder<HaplotypeFrequencySetMetadata>.New
            .With(x => x.EthnicityCode, "ethnicity")
            .With(x => x.Name, "name")
            .With(x => x.RegistryCode, "registry");
    }
}
