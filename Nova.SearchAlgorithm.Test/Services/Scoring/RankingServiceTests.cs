using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
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
                ScoreResult = new ScoreResultBuilder().Build()
            };
            
            var resultWithFewerMismatches = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder()
                    .WithMatchCountAtLocus(Locus.A, 2)
                    .WithMatchCountAtLocus(Locus.B, 2)
                    .WithMatchCountAtLocus(Locus.Drb1, 2)
                    .Build(),
                ScoreResult = new ScoreResultBuilder().Build()
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
        
        [Test]
        public void RankSearchResults_OrdersResultsByMatchGrade()
        {
            var resultWithBetterOverallGrade = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder().Build(),
                ScoreResult = new ScoreResultBuilder()
                    .WithMatchGradeAtLocus(Locus.A, MatchGrade.GDna)
                    .WithMatchGradeAtLocus(Locus.B, MatchGrade.GDna)
                    .Build()
            };
            
            var resultWithWorseOverallGrade = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder().Build(),
                ScoreResult = new ScoreResultBuilder()
                    .WithMatchGradeAtLocus(Locus.B, MatchGrade.Split)
                    .WithMatchGradeAtLocus(Locus.Drb1, MatchGrade.Associated)
                    .Build()
            };

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
        public void RankSearchResults_OrdersResultsByMismatchCountBeforeMatchGrade()
        {
            var resultWithBetterOverallGradeButFewerMatches = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder()
                    .WithMatchCountAtLocus(Locus.A, 1)
                    .Build(),
                ScoreResult = new ScoreResultBuilder()
                    .WithMatchGradeAtLocus(Locus.A, MatchGrade.GDna)
                    .WithMatchGradeAtLocus(Locus.B, MatchGrade.GDna)
                    .Build()
            };
            
            var resultWithWorseOverallGradeButMoreMatches = new MatchAndScoreResult
            {
                MatchResult = new MatchResultBuilder()
                    .WithMatchCountAtLocus(Locus.A, 2)
                    .Build(),
                ScoreResult = new ScoreResultBuilder()
                    .WithMatchGradeAtLocus(Locus.B, MatchGrade.Split)
                    .WithMatchGradeAtLocus(Locus.Drb1, MatchGrade.Associated)
                    .Build()
            };

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
    }
}