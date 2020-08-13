using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers
{
    [Builder]
    internal static class SimulantBuilder
    {
        private const string DefaultHla = "default-hla";

        internal static Builder<Simulant> New => 
            Builder<Simulant>.New.WithFactory(x => x.Id, IncrementingIdGenerator.NextIntId);

        internal static Builder<Simulant> Default => New.WithHlaAtEveryLocus(DefaultHla);

        internal static Builder<Simulant> WithHlaAtEveryLocus(this Builder<Simulant> builder, string hla)
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
}
