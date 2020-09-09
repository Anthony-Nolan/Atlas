using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class PdpPredictionBuilder
    {
        public static Builder<PdpPrediction> Default =>
            Builder<PdpPrediction>.New
                .WithFactory(x => x.PatientGenotypeSimulantId, IncrementingIdGenerator.NextIntId)
                .WithFactory(x => x.DonorGenotypeSimulantId, IncrementingIdGenerator.NextIntId);
    }
}
