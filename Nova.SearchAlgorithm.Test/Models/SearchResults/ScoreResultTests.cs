using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Builders.SearchResults;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Models.SearchResults
{
    [TestFixture]
    public class ScoreResultTests
    {
        [Test]
        public void OverallMatchConfidence_WhenAllMatchConfidencesEqual_ReturnsMatchConfidence()
        {
            const MatchConfidence matchConfidence = MatchConfidence.Exact;
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtLocus(Locus.A, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.B, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.C, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, matchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Drb1, matchConfidence)
                .Build();

            var overallMatchConfidence = scoreResult.OverallMatchConfidence;

            overallMatchConfidence.Should().Be(matchConfidence);
        }
        
        [Test]
        public void OverallMatchConfidence_WhenMatchConfidencesDifferPerLocus_ReturnsLowestMatchConfidence()
        {
            const MatchConfidence higherMatchConfidence = MatchConfidence.Exact;
            const MatchConfidence lowerMatchConfidence = MatchConfidence.Potential;
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtLocus(Locus.A, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.B, lowerMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.C, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Drb1, higherMatchConfidence)
                .Build();

            var overallMatchConfidence = scoreResult.OverallMatchConfidence;

            overallMatchConfidence.Should().Be(lowerMatchConfidence);
        }     
        
        [Test]
        public void OverallMatchConfidence_WhenMatchConfidencesDifferPerPosition_ReturnsLowestMatchConfidence()
        {
            const MatchConfidence higherMatchConfidence = MatchConfidence.Exact;
            const MatchConfidence lowerMatchConfidence = MatchConfidence.Potential;
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtLocus(Locus.A, higherMatchConfidence)
                .WithMatchConfidenceAtLocusPosition(Locus.B, TypePosition.One, higherMatchConfidence)
                .WithMatchConfidenceAtLocusPosition(Locus.B, TypePosition.Two, lowerMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.C, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Drb1, higherMatchConfidence)
                .Build();

            var overallMatchConfidence = scoreResult.OverallMatchConfidence;

            overallMatchConfidence.Should().Be(lowerMatchConfidence);
        }
    }
    
}