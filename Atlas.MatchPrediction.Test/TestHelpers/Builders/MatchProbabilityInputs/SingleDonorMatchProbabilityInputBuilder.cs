using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability.SingleDonorMatchProbabilityInput>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;

public static class SingleDonorMatchProbabilityInputBuilder
{
    public static Composer Default => FixtureBuilder.For<SingleDonorMatchProbabilityInput>()
        .With(i => i.Donor, new DonorInput { DonorId = 123 })
        .WithPatientHla(new PhenotypeInfo<string>("hla"))
        .WithDonorHla(new PhenotypeInfo<string>("hla"));

    /// <summary>
    /// Has all the required fields set
    /// </summary>
    public static Composer Valid => FixtureBuilder.For<SingleDonorMatchProbabilityInput>()
        .WithExcludedLoci(Locus.Dpb1)
        .WithPatientHla(new PhenotypeInfo<string>("hla"))
        .With(i => i.Donor, new DonorInput { DonorId = 123 })
        .WithDonorHla(new PhenotypeInfo<string>("hla"));

    public static Composer WithDonorId(this Composer builder, int id)
    {
        var donorInput = builder.Build().Donor;
        donorInput.DonorId = id;
        return builder.With(i => i.Donor, donorInput);
    }

    public static Composer WithDonorHla(this Composer builder, PhenotypeInfo<string> donorHla)
    {
        var donorInput = builder.Build().Donor;
        donorInput.DonorHla = donorHla?.ToPhenotypeInfoTransfer();
        return builder.With(i => i.Donor, donorInput);
    }

    public static Composer WithDonorMetadata(this Composer builder, FrequencySetMetadata frequencySetMetadata)
    {
        var donorInput = builder.Build().Donor;
        donorInput.DonorFrequencySetMetadata = frequencySetMetadata;
        return builder.With(i => i.Donor, donorInput);
    }

    public static Composer WithPatientHla(this Composer builder, PhenotypeInfo<string> patientHla) =>
        builder.With(i => i.PatientHla, patientHla?.ToPhenotypeInfoTransfer());

    public static Composer WithPatientMetadata(this Composer builder, FrequencySetMetadata frequencySetMetadata) =>
        builder.With(i => i.PatientFrequencySetMetadata, frequencySetMetadata);

    public static Composer WithExcludedLoci(this Composer builder, params Locus[] loci) => builder.With(i => i.ExcludedLoci, loci);
}