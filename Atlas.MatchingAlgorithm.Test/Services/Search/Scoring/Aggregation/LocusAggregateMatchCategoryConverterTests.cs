using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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

        [TestCase(Dpb1TceGroupMatchType.NonPermissiveGvH, MismatchDirection.NonPermissiveGvH)]
        [TestCase(Dpb1TceGroupMatchType.NonPermissiveHvG, MismatchDirection.NonPermissiveHvG)]
        [TestCase(Dpb1TceGroupMatchType.Permissive, MismatchDirection.NotApplicable)]
        public void GetMismatchDirection_ReturnsMismatchDirection(Dpb1TceGroupMatchType dpb1TceGroupMatchType, MismatchDirection expectedMismatchDirection)
        {
            var result = LocusMatchCategoryAggregator.GetMismatchDirection(dpb1TceGroupMatchType);

            result.Should().Be(expectedMismatchDirection);
        }

        [Test]
        public void GetMismatchDirection_ReturnsNull_WhenMatchTypeIsUnknown()
        {
            var result = LocusMatchCategoryAggregator.GetMismatchDirection(Dpb1TceGroupMatchType.Unknown);

            result.Should().BeNull();
        }
    }
}