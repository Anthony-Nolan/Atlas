using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class SimulantBuilder
{
    private const string DefaultHla = "default-hla";

    internal static IPostprocessComposer<Simulant> New =>
        FixtureBuilder.For<Simulant>().With(x => x.Id, IncrementingIdGenerator.NextIntId);

    internal static IPostprocessComposer<Simulant> Default => New.WithHlaAtEveryLocus(DefaultHla);

    internal static IPostprocessComposer<Simulant> WithHlaAtEveryLocus(this IPostprocessComposer<Simulant> builder, string hla)
    {
        return builder
            .With(x => x.A_1, hla)
            .With(x => x.A_2, hla)
            .With(x => x.B_1, hla)
            .With(x => x.B_2, hla)
            .With(x => x.C_1, hla)
            .With(x => x.C_2, hla)
            .With(x => x.DQB1_1, hla)
            .With(x => x.DQB1_2, hla)
            .With(x => x.DRB1_1, hla)
            .With(x => x.DRB1_2, hla);
    }
}