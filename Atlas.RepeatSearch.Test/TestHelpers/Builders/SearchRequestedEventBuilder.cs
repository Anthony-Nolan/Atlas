﻿using Atlas.SearchTracking.Common.Models;
using LochNessBuilder;
using System;

namespace Atlas.RepeatSearch.Test.TestHelpers.Builders
{
    [Builder]
    internal static class SearchRequestedEventBuilder
    {
        public static Builder<SearchRequestedEvent> New =>
            Builder<SearchRequestedEvent>.New
                .With(x => x.SearchIdentifier, new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"))
                .With(x => x.IsRepeatSearch, true)
                .With(x => x.OriginalSearchIdentifier, new Guid("11111111-2222-3333-4444-555555555555"))
                .With(x => x.RepeatSearchCutOffDate,
                    new DateTime(2024, 5, 11, 14, 30, 45, 0, 0, DateTimeKind.Utc))
                .With(x => x.SearchCriteria, "6/6")
                .With(x => x.DonorType, "Adult")
                .With(x => x.RequestTimeUtc, DateTime.UtcNow)
                .With(x => x.AreBetterMatchesIncluded, true)
                .With(x => x.IsMatchPredictionRun, true)
                .With(x => x.DonorRegistryCodes, ["A", "B"]);
    }
}