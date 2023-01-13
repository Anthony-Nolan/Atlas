using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.DonorInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs
{
    internal static class DonorInputBuilder
    {
        public static Builder Default => Builder.New
            .WithHla(new PhenotypeInfo<string>("default-hla"))
            .WithMetadata(FrequencySetMetadataBuilder.New.Build())
            .WithFactory(d => d.DonorIds, () => new List<int> {IncrementingIdGenerator.NextIntId()});

        public static Builder WithHla(this Builder builder, PhenotypeInfo<string> phenotypeInfo) =>
            builder.With(d => d.DonorHla, phenotypeInfo.ToPhenotypeInfoTransfer());

        public static Builder WithMetadata(this Builder builder, FrequencySetMetadata frequencySetMetadata) =>
            builder.With(d => d.DonorFrequencySetMetadata, frequencySetMetadata);

        public static Builder WithDonorIds(this Builder builder, params int[] donorIds) => builder.With(d => d.DonorIds, donorIds.ToList());
    }
}