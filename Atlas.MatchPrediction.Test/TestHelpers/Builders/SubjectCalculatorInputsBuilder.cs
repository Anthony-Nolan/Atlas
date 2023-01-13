using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchPrediction.Services.MatchProbability.SubjectCalculatorInputs>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders
{
    [Builder]
    internal static class SubjectCalculatorInputsBuilder
    {
        public static Builder New => Builder.New
            .With(i => i.Genotypes, new HashSet<PhenotypeInfo<string>>())
            .With(i => i.GenotypeLikelihoods, new Dictionary<PhenotypeInfo<string>, decimal>());

        public static Builder WithLikelihoods(this Builder builder, IReadOnlyDictionary<PhenotypeInfo<string>, decimal> likelihoods)
        {
            return builder.With(i => i.GenotypeLikelihoods, likelihoods);
        }

        public static Builder WithGenotypes(this Builder builder, params PhenotypeInfo<string>[] genotypes)
        {
            return builder.With(i => i.Genotypes, new HashSet<PhenotypeInfo<string>>(genotypes));
        } 
    }
}