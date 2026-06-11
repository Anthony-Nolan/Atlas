using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.DonorInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;

internal static class DonorInputBuilder
{
    public static Composer Default => FixtureBuilder.For<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.DonorInput>()
        .WithHla(new PhenotypeInfo<string>("default-hla"))
        .WithMetadata(FrequencySetMetadataBuilder.New.Build())
        .With(d => d.DonorIds, () => new List<int> {IncrementingIdGenerator.NextIntId()});

    public static Composer WithHla(this Composer builder, PhenotypeInfo<string> phenotypeInfo) =>
        builder.With(d => d.DonorHla, phenotypeInfo.ToPhenotypeInfoTransfer());

    public static Composer WithMetadata(this Composer builder, FrequencySetMetadata frequencySetMetadata) =>
        builder.With(d => d.DonorFrequencySetMetadata, frequencySetMetadata);

    public static Composer WithDonorIds(this Composer builder, params int[] donorIds) => builder.With(d => d.DonorIds, donorIds.ToList());
}