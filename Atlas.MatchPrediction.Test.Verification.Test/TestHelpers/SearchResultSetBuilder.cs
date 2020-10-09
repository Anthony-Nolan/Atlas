using System.Collections.Generic;
using Atlas.Client.Models.Search.Results;
using LochNessBuilder;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers
{
    [Builder]
    internal static class SearchResultSetBuilder
    {
        public static Builder<SearchResultSet> New => Builder<SearchResultSet>.New;

        public static Builder<SearchResultSet> Empty => New
            .With(x => x.SearchResults, new List<SearchResult>());

        public static Builder<SearchResultSet> WithSearchResult(this Builder<SearchResultSet> builder, int donorId)
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