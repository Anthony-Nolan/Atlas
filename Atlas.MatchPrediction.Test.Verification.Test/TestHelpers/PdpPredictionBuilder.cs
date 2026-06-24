using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class PdpPredictionBuilder
{
    public static IPostprocessComposer<PdpPrediction> Default =>
        FixtureBuilder.For<PdpPrediction>()
            .With(x => x.PatientGenotypeSimulantId, IncrementingIdGenerator.NextIntId)
            .With(x => x.DonorGenotypeSimulantId, IncrementingIdGenerator.NextIntId);
}