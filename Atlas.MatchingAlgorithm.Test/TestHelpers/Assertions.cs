using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using FluentAssertions;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers
{
    public static class Assertions
    {
        public static void ShouldContainDonor(this IEnumerable<MatchResult> matchResults, int donorId)
        {
            var donorIds = matchResults.Select(r => r.DonorInfo.DonorId);
            donorIds.Should().Contain(donorId);
        }

        public static void ShouldContainDonor(this IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults, int donorId)
        {
            var donorIds = matchingAlgorithmResults.Select(r => r.AtlasDonorId);
            donorIds.Should().Contain(donorId);
        }

        public static void ShouldNotContainDonor(this IEnumerable<MatchingAlgorithmResult> matchingAlgorithmResults, int donorId)
        {
            var donorIds = matchingAlgorithmResults.Select(r => r.AtlasDonorId);
            donorIds.Should().NotContain(donorId);
        }
    }
}