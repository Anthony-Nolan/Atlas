using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.MatchProbabilityRequestInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs
{
    [Builder]
    internal static class MatchProbabilityRequestInputBuilder
    {
        public static Builder Default => Builder.New
            .WithPatientHla(new PhenotypeInfo<string>("hla"))
            .WithPatientMetadata(FrequencySetMetadataBuilder.New.Build())
            .WithHlaNomenclature("nomenclature-version");

        public static Builder WithPatientHla(this Builder builder, PhenotypeInfo<string> patientHla) =>
            builder.With(i => i.PatientHla, patientHla.ToPhenotypeInfoTransfer());

        public static Builder WithPatientMetadata(this Builder builder, FrequencySetMetadata frequencySetMetadata) =>
            builder.With(i => i.PatientFrequencySetMetadata, frequencySetMetadata);

        public static Builder WithHlaNomenclature(this Builder builder, string nomenclatureVersion) =>
            builder.With(i => i.HlaNomenclatureVersion, nomenclatureVersion);

        public static Builder WithExcludedLoci(this Builder builder, params Locus[] loci) => builder.With(i => i.ExcludedLoci, loci);
        public static Builder WithSearchRequestId(this Builder builder, string requestId) => builder.With(i => i.SearchRequestId, requestId);
    }
}