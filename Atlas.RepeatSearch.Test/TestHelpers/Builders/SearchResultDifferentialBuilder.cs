using System.Collections.Generic;
using System.Linq;
using Atlas.DonorImport.ExternalInterface.Models;
using LochNessBuilder;
using Builder = LochNessBuilder.Builder<Atlas.RepeatSearch.Services.Search.SearchResultDifferential>;

namespace Atlas.RepeatSearch.Test.TestHelpers.Builders
{
    [Builder]
    internal static class SearchResultDifferentialBuilder
    {
        public static Builder New => Builder.New
            .With(d => d.NewResults, new List<DonorIdPair>())
            .With(d => d.UpdatedResults, new List<DonorIdPair>())
            .With(d => d.RemovedResults, new List<string>());

        public static Builder WithRemovedResults(this Builder builder, params string[] donorCodes)
            => builder.With(d => d.RemovedResults, donorCodes.ToList());

        public static Builder WithNewResults(this Builder builder, params string[] donorCodes)
            => builder.With(d => d.NewResults, donorCodes.Select(code => new DonorIdPair {ExternalDonorCode = code}).ToList());

        public static Builder WithUpdatedResults(this Builder builder, params string[] donorCodes)
            => builder.With(d => d.UpdatedResults, donorCodes.Select(code => new DonorIdPair {ExternalDonorCode = code}).ToList());
    }
}