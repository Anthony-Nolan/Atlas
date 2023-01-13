using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using LochNessBuilder;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults
{
    [Builder]
    public static class ScoreResultAggregatorParametersBuilder
    {
        public static Builder<ScoreResultAggregatorParameters> New =>
            Builder<ScoreResultAggregatorParameters>.New
                .With(x => x.ScoreResult, new ScoreResultBuilder().Build())
                .With(x => x.ScoredLoci, EnumerateValues<Locus>().ToList())
                .With(x => x.LociToExclude, new List<Locus>());
    }
}
