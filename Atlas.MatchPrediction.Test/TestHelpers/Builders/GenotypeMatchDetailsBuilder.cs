using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.Models.GenotypeMatchDetails>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders;

internal static class GenotypeMatchDetailsBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.MatchPrediction.Models.GenotypeMatchDetails>();

    public static Composer WithGenotypes(
        this Composer builder,
        PhenotypeInfo<string> donorGenotype,
        PhenotypeInfo<string> patientGenotype) =>
        builder
            .With(gmd => gmd.DonorGenotype, donorGenotype)
            .With(gmd => gmd.PatientGenotype, patientGenotype);

    public static Composer WithMatchCounts(this Composer builder, LociInfo<int?> matchCounts) =>
        builder.With(gmd => gmd.MatchCounts, matchCounts);

    public static Composer WithAvailableLoci(this Composer builder, ISet<Locus> availableLoci) =>
        builder.With(gmd => gmd.AvailableLoci, availableLoci);

    public static Composer WithDonorGenotypeLikelihood(this Composer builder, decimal likelihood) =>
        builder.With(gmd => gmd.DonorGenotypeLikelihood, likelihood);

    public static Composer WithPatientGenotypeLikelihood(this Composer builder, decimal likelihood) =>
        builder.With(gmd => gmd.PatientGenotypeLikelihood, likelihood);
}