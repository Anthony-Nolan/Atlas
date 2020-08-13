using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Test.Verification.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers
{
    [Builder]
    internal static class SimulantLocusHlaBuilder
    {
        internal static Builder<SimulantLocusHla> New => Builder<SimulantLocusHla>.New;

        internal static Builder<SimulantLocusHla> WithIncrementingIds(this Builder<SimulantLocusHla> builder)
        {
            return builder.WithFactory(x => x.GenotypeSimulantId, IncrementingIdGenerator.NextIntId);
        }

        internal static Builder<SimulantLocusHla> WithTypingFromLocusName(this Builder<SimulantLocusHla> builder, Locus locus)
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
}