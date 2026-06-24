using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture.Dsl;

namespace Atlas.MatchPrediction.Test.Verification.Test.TestHelpers;

internal static class MatchingResultSetBuilder
{
    public static IPostprocessComposer<OriginalMatchingAlgorithmResultSet> New => FixtureBuilder.For<OriginalMatchingAlgorithmResultSet>();

    public static IPostprocessComposer<OriginalMatchingAlgorithmResultSet> Empty => New
        .With(x => x.Results, new List<MatchingAlgorithmResult>());

    public static IPostprocessComposer<OriginalMatchingAlgorithmResultSet> WithMatchingResult(this IPostprocessComposer<OriginalMatchingAlgorithmResultSet> builder, int donorId)
    {
        return builder.With(x => x.Results, new[] { BuildMatchingAlgorithmResult(donorId) });
    }

    private static MatchingAlgorithmResult BuildMatchingAlgorithmResult(int donorId)
    {
        return new MatchingAlgorithmResult
        {
            DonorCode = donorId.ToString()
        };
    }
}