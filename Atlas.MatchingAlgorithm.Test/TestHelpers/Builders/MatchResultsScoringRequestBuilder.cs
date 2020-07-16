using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using LochNessBuilder;
using System.Linq;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    public static class MatchResultsScoringRequestBuilder
    {
        public static Builder<MatchResultsScoringRequest> New =>
            Builder<MatchResultsScoringRequest>.New
                .With(x => x.PatientHla, new PhenotypeInfo<string>())
                .With(x => x.MatchResults, new[] { new MatchResultBuilder().Build() })
                .With(x => x.LociToScore, EnumerateValues<Locus>().ToList());
    }
}
