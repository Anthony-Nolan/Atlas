using System.Collections.Generic;
using FluentAssertions;
using LochNessBuilder;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Services.Search.Scoring.Ranking;
using Nova.SearchAlgorithm.Test.Builders.SearchResults;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Search.Scoring
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
                .WithAggregateScoringData(Builder<AggregateScoreDetails>.New.With(x => x.MatchCount, 1).Build())
                .Build();

            var resultWithMoreMatches = new MatchAndScoreResultBuilder()
                .WithAggregateScoringData(Builder<AggregateScoreDetails>.New.With(x => x.MatchCount, 5).Build())
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
                .WithAggregateScoringData(Builder<AggregateScoreDetails>.New.With(x => x.GradeScore, 100).Build())
                .Build();

            var resultWithWorseOverallGrade = new MatchAndScoreResultBuilder()
                .WithAggregateScoringData(Builder<AggregateScoreDetails>.New.With(x => x.GradeScore, 1).Build())
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
                .WithAggregateScoringData(
                    Builder<AggregateScoreDetails>.New
                    .With(x => x.GradeScore, 100)
                    .With(x => x.MatchCount, 1)
                    .Build()
                )
                .Build();

            var resultWithWorseOverallGradeButMoreMatches = new MatchAndScoreResultBuilder()
                .WithAggregateScoringData(
                    Builder<AggregateScoreDetails>.New
                        .With(x => x.GradeScore, 1)
                        .With(x => x.MatchCount, 6)
                        .Build()
                )
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
                .WithAggregateScoringData(Builder<AggregateScoreDetails>.New.With(x => x.ConfidenceScore, 100).Build())
                .Build();

            var resultWithWorseOverallConfidence = new MatchAndScoreResultBuilder()
                .WithAggregateScoringData(Builder<AggregateScoreDetails>.New.With(x => x.ConfidenceScore, 1).Build())
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
                .WithAggregateScoringData(
                    Builder<AggregateScoreDetails>.New
                        .With(x => x.GradeScore, 100)
                        .With(x => x.ConfidenceScore, 1)
                        .Build()
                )
                .Build();

            var resultWithBetterConfidenceButWorseGrade = new MatchAndScoreResultBuilder()
                .WithAggregateScoringData(
                    Builder<AggregateScoreDetails>.New
                        .With(x => x.GradeScore, 1)
                        .With(x => x.ConfidenceScore, 100)
                        .Build()
                )
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
                .WithAggregateScoringData(
                    Builder<AggregateScoreDetails>.New
                        .With(x => x.ConfidenceScore, 5)
                        .With(x => x.MatchCount, 6)
                        .Build()
                )
                .Build();

            var resultWithBetterConfidenceButWorseMatchCount = new MatchAndScoreResultBuilder()
                .WithAggregateScoringData(
                    Builder<AggregateScoreDetails>.New
                        .With(x => x.ConfidenceScore, 500)
                        .With(x => x.MatchCount, 1)
                        .Build()
                )
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