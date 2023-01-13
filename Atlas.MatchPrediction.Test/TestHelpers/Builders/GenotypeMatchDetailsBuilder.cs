using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Models.GenotypeMatchDetails>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class GenotypeMatchDetailsBuilder
    {
        public static Builder New => Builder.New;

        public static Builder WithGenotypes(
            this Builder builder,
            PhenotypeInfo<string> donorGenotype,
            PhenotypeInfo<string> patientGenotype) =>
            builder
                .With(gmd => gmd.DonorGenotype, donorGenotype)
                .With(gmd => gmd.PatientGenotype, patientGenotype);

        public static Builder WithMatchCounts(this Builder builder, LociInfo<int?> matchCounts) =>
            builder.With(gmd => gmd.MatchCounts, matchCounts);

        public static Builder WithAvailableLoci(this Builder builder, ISet<Locus> availableLoci) =>
            builder.With(gmd => gmd.AvailableLoci, availableLoci);

        public static Builder WithDonorGenotypeLikelihood(this Builder builder, decimal likelihood) =>
            builder.With(gmd => gmd.DonorGenotypeLikelihood, likelihood);

        public static Builder WithPatientGenotypeLikelihood(this Builder builder, decimal likelihood) =>
            builder.With(gmd => gmd.PatientGenotypeLikelihood, likelihood);
    }
}