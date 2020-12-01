using System.Collections.Generic;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.MatchingAlgorithm.Common.Models.AlleleLevelLocusMatchCriteria>;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests
{
    [Builder]
    public static class AlleleLevelLocusMatchCriteriaBuilder
    {
        public static Builder New => Builder.New;

        public static Builder WithMismatchCount(this Builder builder, int mismatchCount) => builder
            .With(c => c.MismatchCount, mismatchCount);

        public static Builder WithPGroups(this Builder builder, IEnumerable<string> pGroups1, IEnumerable<string> pGroups2) => builder
            .With(c => c.PGroupsToMatchInPositionOne, pGroups1)
            .With(c => c.PGroupsToMatchInPositionTwo, pGroups2);
    }
}