using System.Collections.Generic;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.ResultSet;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class SearchResultSetBuilder
    {
        public static Builder<OriginalSearchResultSet> New => Builder<OriginalSearchResultSet>.New;

        public static Builder<OriginalSearchResultSet> Empty => New
            .With(x => x.SearchResults, new List<SearchResult>());

        public static Builder<OriginalSearchResultSet> WithSearchResult(this Builder<OriginalSearchResultSet> builder, int donorId)
        {
            return builder.With(x => x.SearchResults, new[] { BuildSearchResult(donorId) });
        }

        private static SearchResult BuildSearchResult(int donorId)
        {
            return new SearchResult
            {
                DonorCode = donorId.ToString()
            };
        }
    }
}