using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Aggregation
{
    [TestFixture]
    public class LocusAggregateMatchCategoryConverterTests
    {
        [TestCase(MatchGrade.CDna, MatchGrade.CDna, LocusMatchCategory.Match)]
        [TestCase(MatchGrade.GDna, MatchGrade.GGroup, LocusMatchCategory.Match)]
        [TestCase(MatchGrade.PermissiveMismatch, MatchGrade.CDna, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(MatchGrade.CDna, MatchGrade.PermissiveMismatch, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(MatchGrade.PermissiveMismatch, MatchGrade.PermissiveMismatch, LocusMatchCategory.PermissiveMismatch)]
        [TestCase(MatchGrade.CDna, MatchGrade.Mismatch, LocusMatchCategory.Mismatch)]
        [TestCase(MatchGrade.Mismatch, MatchGrade.CDna, LocusMatchCategory.Mismatch)]
        [TestCase(MatchGrade.PermissiveMismatch, MatchGrade.Mismatch, LocusMatchCategory.Mismatch)]
        [TestCase(MatchGrade.Mismatch, MatchGrade.PermissiveMismatch, LocusMatchCategory.Mismatch)]
        [TestCase(MatchGrade.Mismatch, MatchGrade.Mismatch, LocusMatchCategory.Mismatch)]
        [TestCase(MatchGrade.Unknown, MatchGrade.Unknown, LocusMatchCategory.Unknown)]
        public void LocusAggregateMatchCategoryConverter_ReturnsMatchCategoryByWorstMatchGrade(
            MatchGrade position1Grade,
            MatchGrade position2Grade,
            LocusMatchCategory expectedLocusMatchCategory
        )
        {
            var locusScore = new LocusScoreDetailsBuilder()
                .WithMatchGradeAtPosition(LocusPosition.One, position1Grade)
                .WithMatchGradeAtPosition(LocusPosition.Two, position2Grade)
                .Build();

            LocusMatchCategoryAggregator
                .LocusMatchCategoryFromPositionScores(new LocusInfo<LocusPositionScoreDetails>(
                    locusScore.ScoreDetailsAtPosition1,
                    locusScore.ScoreDetailsAtPosition2))
                .Should().Be(expectedLocusMatchCategory);
        }

        [Test]
        public void LocusAggregateMatchCategoryConverter_ThrowsErrorIfOnlyOneInputIsUnknown()
        {
            var locusScore = new LocusScoreDetailsBuilder()
                .WithMatchGradeAtPosition(LocusPosition.One, MatchGrade.Unknown)
                .WithMatchGradeAtPosition(LocusPosition.Two, MatchGrade.Mismatch)
                .Build();

            Assert.Throws<ArgumentException>(() =>
                LocusMatchCategoryAggregator.LocusMatchCategoryFromPositionScores(new LocusInfo<LocusPositionScoreDetails>(
                    locusScore.ScoreDetailsAtPosition1,
                    locusScore.ScoreDetailsAtPosition2)
                ));
        }
    }
}