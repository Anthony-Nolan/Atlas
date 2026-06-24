using System;
using Atlas.SearchTracking.Common.Models;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;

internal static class SearchRequestedEventBuilder
{
    public static IPostprocessComposer<SearchRequestedEvent> New =>
        FixtureBuilder.For<SearchRequestedEvent>()
            .With(x => x.SearchIdentifier, new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
            .With(x => x.IsRepeatSearch, false)
            .With(x => x.SearchCriteria, "6/6")
            .With(x => x.DonorType, "Adult")
            .With(x => x.RequestTimeUtc, DateTime.UtcNow)
            .With(x => x.IsMatchPredictionRun, true)
            .With(x => x.AreBetterMatchesIncluded, true)
            .With(x => x.DonorRegistryCodes, ["A", "B"]);
}