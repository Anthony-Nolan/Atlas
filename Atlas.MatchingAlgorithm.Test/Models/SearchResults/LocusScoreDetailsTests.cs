using System;
using Atlas.Client.Models.Common.Results;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Models.SearchResults
{
    [TestFixture]
    public class LocusScoreDetailsTests
    {
        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void MatchGradeScore_WhenMatchGradeScoreIsNull_ThrowsException(LocusPosition typePosition)
        {
            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchGradeScoreAtPosition(typePosition, null)
                .Build();

            Assert.Throws<Exception>(() =>
            {
                var matchGradeScore = locusScoreDetails.MatchGradeScore;
            });
        }

        [Test]
        public void MatchGradeScore_WhenMatchGradeScoresAreNotNull_ReturnsSumOfMatchGradeScoresAtPositionOneAndTwo()
        {
            const int scoreAtPositionOne = 1;
            const int scoreAtPositionTwo = 2;
            const int expectedTotalScore = 3;

            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchGradeScoreAtPosition(LocusPosition.One, scoreAtPositionOne)
                .WithMatchGradeScoreAtPosition(LocusPosition.Two, scoreAtPositionTwo)
                .Build();

            locusScoreDetails.MatchGradeScore.Should().Be(expectedTotalScore);
        }

        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void MatchConfidenceScore_WhenMatchConfidenceScoreIsNull_ThrowsException(LocusPosition position)
        {
            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchConfidenceScoreAtPosition(position, null)
                .Build();

            Assert.Throws<Exception>(() =>
            {
                var matchConfidenceScore = locusScoreDetails.MatchConfidenceScore;
            });
        }

        [Test]
        public void MatchConfidenceScore_WhenMatchConfidenceScoresAreNotNull_ReturnsSumOfMatchConfidenceScoresAtPositionOneAndTwo()
        {
            const int scoreAtPositionOne = 1;
            const int scoreAtPositionTwo = 2;
            const int expectedTotalScore = 3;

            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchConfidenceScoreAtPosition(LocusPosition.One, scoreAtPositionOne)
                .WithMatchConfidenceScoreAtPosition(LocusPosition.Two, scoreAtPositionTwo)
                .Build();

            locusScoreDetails.MatchConfidenceScore.Should().Be(expectedTotalScore);
        }

        [TestCase(MatchConfidence.Potential, MatchConfidence.Potential, 2)]
        [TestCase(MatchConfidence.Potential, MatchConfidence.Definite, 1)]
        [TestCase(MatchConfidence.Potential, MatchConfidence.Exact, 1)]
        [TestCase(MatchConfidence.Potential, MatchConfidence.Mismatch, 1)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Potential, 1)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Definite, 0)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Exact, 0)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Mismatch, 0)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Potential, 1)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Definite, 0)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Exact, 0)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Mismatch, 0)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Potential, 1)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Definite, 0)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Exact, 0)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Mismatch, 0)]
        public void PotentialMatchCount_CalculatesPotentialMatchCount(
            MatchConfidence matchConfidenceAtOne,
            MatchConfidence matchConfidenceAtTwo,
            int expectedCount)
        {
            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchConfidenceAtPosition(LocusPosition.One, matchConfidenceAtOne)
                .WithMatchConfidenceAtPosition(LocusPosition.Two, matchConfidenceAtTwo)
                .Build();

            locusScoreDetails.PotentialMatchCount().Should().Be(expectedCount);
        }

        [TestCase(MatchConfidence.Potential, MatchConfidence.Potential)]
        [TestCase(MatchConfidence.Potential, MatchConfidence.Definite)]
        [TestCase(MatchConfidence.Potential, MatchConfidence.Exact)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Potential)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Definite)]
        [TestCase(MatchConfidence.Definite, MatchConfidence.Exact)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Potential)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Definite)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Exact)]
        public void MatchCount_WhenNeitherMatchConfidencesIsMismatch_Returns2(
            MatchConfidence matchConfidenceAtOne,
            MatchConfidence matchConfidenceAtTwo)
        {
            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchConfidenceAtPosition(LocusPosition.One, matchConfidenceAtOne)
                .WithMatchConfidenceAtPosition(LocusPosition.Two, matchConfidenceAtTwo)
                .Build();

            locusScoreDetails.MatchCount().Should().Be(2);
        }

        [TestCase(MatchConfidence.Definite, MatchConfidence.Mismatch)]
        [TestCase(MatchConfidence.Potential, MatchConfidence.Mismatch)]
        [TestCase(MatchConfidence.Exact, MatchConfidence.Mismatch)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Definite)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Potential)]
        [TestCase(MatchConfidence.Mismatch, MatchConfidence.Exact)]
        public void MatchCount_WhenOneMatchConfidenceIsMismatch_Returns1(
            MatchConfidence matchConfidenceAtOne,
            MatchConfidence matchConfidenceAtTwo)
        {
            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchConfidenceAtPosition(LocusPosition.One, matchConfidenceAtOne)
                .WithMatchConfidenceAtPosition(LocusPosition.Two, matchConfidenceAtTwo)
                .Build();

            locusScoreDetails.MatchCount().Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenBothMatchConfidencesAreMismatch_Returns0()
        {
            var locusScoreDetails = new LocusScoreDetailsBuilder()
                .WithMatchConfidenceAtPosition(LocusPosition.One, MatchConfidence.Mismatch)
                .WithMatchConfidenceAtPosition(LocusPosition.Two, MatchConfidence.Mismatch)
                .Build();

            locusScoreDetails.MatchCount().Should().Be(0);
        }
    }
}
