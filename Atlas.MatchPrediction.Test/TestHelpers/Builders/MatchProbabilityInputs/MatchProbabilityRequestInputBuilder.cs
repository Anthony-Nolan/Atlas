using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.IdentifiedMatchProbabilityRequest>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;

internal static class MatchProbabilityRequestInputBuilder
{
    public static Composer Default => FixtureBuilder.For<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.IdentifiedMatchProbabilityRequest>()
        .WithPatientHla(new PhenotypeInfo<string>("hla"))
        .WithPatientMetadata(FrequencySetMetadataBuilder.New.Build());

    public static Composer WithPatientHla(this Composer builder, PhenotypeInfo<string> patientHla) =>
        builder.With(i => i.PatientHla, patientHla.ToPhenotypeInfoTransfer());

    public static Composer WithPatientMetadata(this Composer builder, FrequencySetMetadata frequencySetMetadata) =>
        builder.With(i => i.PatientFrequencySetMetadata, frequencySetMetadata);

    public static Composer WithExcludedLoci(this Composer builder, params Locus[] loci) => builder.With(i => i.ExcludedLoci, loci);
    public static Composer WithSearchRequestId(this Composer builder, string requestId) => builder.With(i => i.SearchRequestId, requestId);
}