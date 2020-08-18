using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Models.SearchResults
{
    [TestFixture]
    public class MatchAndScoreResultTests
    {
        [Test]
        public void PotentialMatchCount_OnlyCountsMatchesOfPotentialMatches()
        {
            const int matchCountAtPotentialLoci = 2;
            var matchAndScoreResult = new MatchAndScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 1)
                .WithMatchCountAtLocus(Locus.B, 1)
                .WithMatchCountAtLocus(Locus.C, 1)
                .WithMatchCountAtLocus(Locus.Dqb1, matchCountAtPotentialLoci)
                .WithMatchCountAtLocus(Locus.Drb1, matchCountAtPotentialLoci)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.B, MatchConfidence.Exact)
                .WithMatchConfidenceAtLocus(Locus.C, MatchConfidence.Mismatch)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, MatchConfidence.Potential)
                .WithMatchConfidenceAtLocus(Locus.Drb1, MatchConfidence.Potential)
                .Build();

            // 2 potential loci
            matchAndScoreResult.PotentialMatchCount.Should().Be(matchCountAtPotentialLoci * 2);
        }
        
        [Test]
        public void PotentialMatchCount_WhenSomeLociAreNotMatched_DoesNotCountUnmatchedLoci()
        {
            
            const int matchCountAtPotentialLoci = 2;
            var matchAndScoreResult = new MatchAndScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, matchCountAtPotentialLoci)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Potential)
                .WithMatchConfidenceAtLocus(Locus.B, MatchConfidence.Potential)
                .WithMatchConfidenceAtLocus(Locus.C, MatchConfidence.Potential)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, MatchConfidence.Potential)
                .WithMatchConfidenceAtLocus(Locus.Drb1, MatchConfidence.Potential)
                .Build();

            matchAndScoreResult.PotentialMatchCount.Should().Be(matchCountAtPotentialLoci);
        }
    }
}