using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Models;
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
        [TestCase(MatchGrade.CDna, MatchGrade.Mismatch, LocusMatchCategory.Mismatch)]
        [TestCase(MatchGrade.Mismatch, MatchGrade.CDna, LocusMatchCategory.Mismatch)]
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

        [TestCase(Dpb1TceGroupMatchType.NonPermissiveGvH, Dpb1MismatchDirection.NonPermissiveGvH)]
        [TestCase(Dpb1TceGroupMatchType.NonPermissiveHvG, Dpb1MismatchDirection.NonPermissiveHvG)]
        [TestCase(Dpb1TceGroupMatchType.Permissive, Dpb1MismatchDirection.NotApplicable)]
        public void GetDpb1MismatchDirection_ReturnsDpb1MismatchDirection(Dpb1TceGroupMatchType dpb1TceGroupMatchType, Dpb1MismatchDirection expectedDpb1MismatchDirection)
        {
            var result = LocusMatchCategoryAggregator.GetDpb1MismatchDirection(dpb1TceGroupMatchType);

            result.Should().Be(expectedDpb1MismatchDirection);
        }

        [Test]
        public void GetDpb1MismatchDirection_ReturnsNull_WhenMatchTypeIsUnknown()
        {
            var result = LocusMatchCategoryAggregator.GetDpb1MismatchDirection(Dpb1TceGroupMatchType.Unknown);

            result.Should().BeNull();
        }
    }
}