using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Scoring.Ranking;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.SearchResults;
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
        public void RankSearchResults_OrdersResultsByMatchCount()
        {
            var resultWithFewerMatches = new MatchAndScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 1)
                .WithMatchCountAtLocus(Locus.B, 1)
                .WithMatchCountAtLocus(Locus.B, 1)
                .Build();

            var resultWithMoreMatches = new MatchAndScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .Build();

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithFewerMatches,
                resultWithMoreMatches
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithMoreMatches,
                resultWithFewerMatches
            });
        }

        [Test]
        public void RankSearchResults_OrdersResultsByMatchGrade()
        {
            var resultWithBetterOverallGrade = new MatchAndScoreResultBuilder()
                .WithMatchGradeScoreAtLocus(Locus.A, 3)
                .Build();

            var resultWithWorseOverallGrade = new MatchAndScoreResultBuilder()
                .WithMatchGradeScoreAtLocus(Locus.A, 1)
                .Build();

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithWorseOverallGrade,
                resultWithBetterOverallGrade,
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithBetterOverallGrade,
                resultWithWorseOverallGrade
            });
        }

        [Test]
        public void RankSearchResults_OrdersResultsByMatchCountBeforeMatchGrade()
        {
            var resultWithBetterOverallGradeButFewerMatches = new MatchAndScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 1)
                .WithMatchGradeScoreAtLocus(Locus.A, 3)
                .Build();

            var resultWithWorseOverallGradeButMoreMatches = new MatchAndScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchGradeScoreAtLocus(Locus.A, 1)
                .Build();

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithBetterOverallGradeButFewerMatches,
                resultWithWorseOverallGradeButMoreMatches,
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithWorseOverallGradeButMoreMatches,
                resultWithBetterOverallGradeButFewerMatches,
            });
        }

        [Test]
        public void RankSearchResults_OrdersResultsByMatchConfidence()
        {
            var resultWithBetterOverallConfidence = new MatchAndScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 4)
                .Build();

            var resultWithWorseOverallConfidence = new MatchAndScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 2)
                .Build();

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithWorseOverallConfidence,
                resultWithBetterOverallConfidence,
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithBetterOverallConfidence,
                resultWithWorseOverallConfidence
            });
        }

        [Test]
        public void RankSearchResults_OrdersResultsByMatchGradeBeforeConfidence()
        {
            var resultWithWorseConfidenceButBetterGrade = new MatchAndScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 1)
                .WithMatchGradeScoreAtLocus(Locus.A, 4)
                .Build();

            var resultWithBetterConfidenceButWorseGrade = new MatchAndScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 5)
                .WithMatchGradeScoreAtLocus(Locus.A, 1)
                .Build();

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithBetterConfidenceButWorseGrade,
                resultWithWorseConfidenceButBetterGrade,
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithWorseConfidenceButBetterGrade,
                resultWithBetterConfidenceButWorseGrade
            });
        }

        [Test]
        public void RankSearchResults_OrdersResultsByMatchCountBeforeConfidence()
        {
            var resultWithWorseConfidenceButBetterMatchCount = new MatchAndScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 1)
                .WithMatchCountAtLocus(Locus.A, 2)
                .Build();

            var resultWithBetterConfidenceButWorseMatchCount = new MatchAndScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 5)
                .WithMatchCountAtLocus(Locus.A, 1)
                .Build();

            var unorderedSearchResults = new List<MatchAndScoreResult>
            {
                resultWithBetterConfidenceButWorseMatchCount,
                resultWithWorseConfidenceButBetterMatchCount,
            };

            var orderedSearchResults = rankingService.RankSearchResults(unorderedSearchResults);

            orderedSearchResults.Should().ContainInOrder(new List<MatchAndScoreResult>
            {
                resultWithWorseConfidenceButBetterMatchCount,
                resultWithBetterConfidenceButWorseMatchCount
            });
        }
    }
}