using System.Collections.Generic;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.MatchingAlgorithm.Common.Models.AlleleLevelLocusMatchCriteria>;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;

public static class AlleleLevelLocusMatchCriteriaBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.MatchingAlgorithm.Common.Models.AlleleLevelLocusMatchCriteria>();

    public static Composer WithMismatchCount(this Composer builder, int mismatchCount) => builder
        .With(c => c.MismatchCount, mismatchCount);

    public static Composer WithPGroups(this Composer builder, IEnumerable<string> pGroups1, IEnumerable<string> pGroups2) => builder
        .With(c => c.PGroupsToMatchInPositionOne, pGroups1)
        .With(c => c.PGroupsToMatchInPositionTwo, pGroups2);
}