using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Requests;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using LochNessBuilder;
using static EnumStringValues.EnumExtensions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    internal static class ScoringCriteriaBuilder
    {
        public static Builder<ScoringCriteria> New =>
            Builder<ScoringCriteria>.New
                .With(x => x.LociToScore, new List<Locus>())
                .With(x => x.LociToExcludeFromAggregateScore, new List<Locus>());

        public static Builder<ScoringCriteria> ScoreAllLoci =>
            New.With(x => x.LociToScore, EnumerateValues<Locus>().ToList());
    }
}
