using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using Atlas.MatchPrediction.Test.Verification.Services.Verification;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class GenotypeSimulantsInfoBuilder
{
    public static IPostprocessComposer<GenotypeSimulantsInfo> New => FixtureBuilder.For<GenotypeSimulantsInfo>();

    public static IPostprocessComposer<GenotypeSimulantsInfo> WithEmptySimulantsInfo => New
        .WithPatients(new List<Simulant>())
        .WithDonors(new List<Simulant>());

    public static IPostprocessComposer<GenotypeSimulantsInfo> WithPatient(this IPostprocessComposer<GenotypeSimulantsInfo> builder, Simulant simulant)
    {
        return builder.WithPatients(new[] { simulant });
    }

    public static IPostprocessComposer<GenotypeSimulantsInfo> WithDonor(this IPostprocessComposer<GenotypeSimulantsInfo> builder, Simulant simulant)
    {
        return builder.WithDonors(new[] { simulant });
    }

    private static IPostprocessComposer<GenotypeSimulantsInfo> WithPatients(this IPostprocessComposer<GenotypeSimulantsInfo> builder, IReadOnlyCollection<Simulant> simulants)
    {
        return builder.With(x => x.Patients, BuildSimulantsInfo(simulants));
    }

    private static IPostprocessComposer<GenotypeSimulantsInfo> WithDonors(this IPostprocessComposer<GenotypeSimulantsInfo> builder, IReadOnlyCollection<Simulant> simulants)
    {
        return builder.With(x => x.Donors, BuildSimulantsInfo(simulants));
    }

    private static SimulantsInfo BuildSimulantsInfo(IReadOnlyCollection<Simulant> simulants)
    {
        return new SimulantsInfo
        {
            Hla = simulants,
            Ids = simulants.Select(s => s.Id).ToList()
        };
    }
}