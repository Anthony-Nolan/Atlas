using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.ExternalInterface.Models;
using Composer = AutoFixture.Dsl.IPostprocessComposer<Atlas.RepeatSearch.Services.Search.SearchResultDifferential>;

namespace Atlas.RepeatSearch.Test.TestHelpers.Builders;

internal static class SearchResultDifferentialBuilder
{
    public static Composer New => FixtureBuilder.For<Atlas.RepeatSearch.Services.Search.SearchResultDifferential>()
        .With(d => d.NewResults, new List<DonorIdPair>())
        .With(d => d.UpdatedResults, new List<DonorIdPair>())
        .With(d => d.RemovedResults, new List<string>());

    public static Composer WithRemovedResults(this Composer builder, params string[] donorCodes)
        => builder.With(d => d.RemovedResults, donorCodes.ToList());

    public static Composer WithNewResults(this Composer builder, params string[] donorCodes)
        => builder.With(d => d.NewResults, donorCodes.Select(code => new DonorIdPair {ExternalDonorCode = code}).ToList());

    public static Composer WithUpdatedResults(this Composer builder, params string[] donorCodes)
        => builder.With(d => d.UpdatedResults, donorCodes.Select(code => new DonorIdPair {ExternalDonorCode = code}).ToList());
}