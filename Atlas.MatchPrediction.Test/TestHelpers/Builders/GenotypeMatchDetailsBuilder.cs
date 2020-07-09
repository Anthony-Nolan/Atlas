using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class GenotypeMatchDetailsBuilder
    {
        public static Builder<GenotypeMatchDetails> New => Builder<GenotypeMatchDetails>.New;

        public static Builder<GenotypeMatchDetails> WithGenotypes(
            this Builder<GenotypeMatchDetails> builder,
            PhenotypeInfo<string> donorGenotype,
            PhenotypeInfo<string> patientGenotype)
        {
            return builder
                .With(gmd => gmd.DonorGenotype, donorGenotype)
                .With(gmd => gmd.PatientGenotype, patientGenotype);
        }

        public static Builder<GenotypeMatchDetails> WithMatchCounts(this Builder<GenotypeMatchDetails> builder, LociInfo<int?> matchCounts)
        {
            return builder
                .With(gmd => gmd.MatchCounts, matchCounts);
        }
    }
}