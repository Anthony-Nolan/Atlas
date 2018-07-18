using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Test.Builders;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring
{
    [TestFixture]
    public class RankingServiceTests
    {
        private IRankingService rankingService;
        
        [SetUp]
        public void SetUp()
        {
            rankingService = new RankingService();
        }

        [Test]
        public void RankSearchResults_OrdersResultsByMismatchCount()
        {
            var resultWithMoreMismatches = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder()
                    .WithMatchCountAtLocus(Locus.A, 2)
                    .WithMatchCountAtLocus(Locus.B, 2)
                    .WithMatchCountAtLocus(Locus.Drb1, 1)
                    .Build(),
            };
            var resultWithFewerMismatches = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder()
                    .WithMatchCountAtLocus(Locus.A, 2)
                    .WithMatchCountAtLocus(Locus.B, 2)
                    .WithMatchCountAtLocus(Locus.Drb1, 2)
                    .Build(),
            };

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithMoreMismatches,
                resultWithFewerMismatches
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithFewerMismatches,
                resultWithMoreMismatches
            });
        }
    }
}