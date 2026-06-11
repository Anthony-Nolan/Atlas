using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchPrediction.Services.MatchProbability.SubjectCalculatorInputs>;

namespace Atlas.MatchPrediction.Test.TestHelpers.Builders;

internal static class SubjectCalculatorInputsBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.MatchPrediction.Services.MatchProbability.SubjectCalculatorInputs>()
        .With(i => i.Genotypes, new HashSet<PhenotypeInfo<string>>())
        .With(i => i.GenotypeLikelihoods, new Dictionary<PhenotypeInfo<string>, decimal>());

    public static Composer WithLikelihoods(this Composer builder, IReadOnlyDictionary<PhenotypeInfo<string>, decimal> likelihoods)
    {
        return builder.With(i => i.GenotypeLikelihoods, likelihoods);
    }

    public static Composer WithGenotypes(this Composer builder, params PhenotypeInfo<string>[] genotypes)
    {
        return builder.With(i => i.Genotypes, new HashSet<PhenotypeInfo<string>>(genotypes));
    }
}