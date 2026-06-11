using System.Linq;
using Atlas.MatchingAlgorithm.Models;
using AutoFixture.Dsl;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;

public static class FailedDonorInfoBuilder
{
    public static IPostprocessComposer<FailedDonorInfo> New() =>
        FixtureBuilder.For<FailedDonorInfo>()
            .WithSequence(x => x.AtlasDonorId, Enumerable.Range(1, int.MaxValue).Select(i => (int?) i));
}