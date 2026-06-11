using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders;

public static class FrequencySetMetadataBuilder
{
    public static IPostprocessComposer<FrequencySetMetadata> New => FixtureBuilder.For<FrequencySetMetadata>();

    public static IPostprocessComposer<FrequencySetMetadata> ForRegistry(this IPostprocessComposer<FrequencySetMetadata> builder, string registryCode)
    {
        return builder.With(d => d.RegistryCode, registryCode);
    }

    public static IPostprocessComposer<FrequencySetMetadata> ForEthnicity(this IPostprocessComposer<FrequencySetMetadata> builder, string ethnicityCode)
    {
        return builder.With(d => d.EthnicityCode, ethnicityCode);
    }
}