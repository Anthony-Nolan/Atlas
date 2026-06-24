using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Models;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class SimulantLocusHlaBuilder
{
    internal static IPostprocessComposer<SimulantLocusHla> New => FixtureBuilder.For<SimulantLocusHla>();

    internal static IPostprocessComposer<SimulantLocusHla> WithIncrementingIds(this IPostprocessComposer<SimulantLocusHla> builder)
    {
        return builder.With(x => x.GenotypeSimulantId, IncrementingIdGenerator.NextIntId);
    }

    internal static IPostprocessComposer<SimulantLocusHla> WithTypingFromLocusName(this IPostprocessComposer<SimulantLocusHla> builder, Locus locus)
    {
        return builder
            .With(x => x.Locus, locus)
            .With(x => x.HlaTyping, BuildLocusHla(locus));
    }

    private static LocusInfo<string> BuildLocusHla(Locus locus)
    {
        return new LocusInfo<string>(BuildHla(locus, 1), BuildHla(locus, 2));
    }

    private static string BuildHla(Locus locus, int position)
    {
        return $"{locus}-{position}";
    }
}