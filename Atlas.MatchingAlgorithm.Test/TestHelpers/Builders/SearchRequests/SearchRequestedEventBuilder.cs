using System;
using Atlas.SearchTracking.Common.Models;
using LochNessBuilder;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders
{
    [Builder]
    internal static class SearchRequestedEventBuilder
    {
        public static Builder<SearchRequestedEvent> New =>
            Builder<SearchRequestedEvent>.New
                .With(x => x.SearchRequestId, new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
                .With(x => x.IsRepeatSearch, false)
                .With(x => x.SearchCriteria, "6/6")
                .With(x => x.DonorType, "Adult")
                .With(x => x.RequestTimeUtc, DateTime.UtcNow);
    }
}
