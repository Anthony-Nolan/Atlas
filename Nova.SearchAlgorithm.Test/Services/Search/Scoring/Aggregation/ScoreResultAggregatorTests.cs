using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Services.Search.Scoring.Aggregation;
using Nova.SearchAlgorithm.Test.Builders.SearchResults;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Search.Scoring.Aggregation
{
    [TestFixture]
    public class ScoreResultAggregatorTests
    {
        private IScoreResultAggregator resultAggregator;
        
        [SetUp]
        public void SetUp()
        {
            resultAggregator = new ScoreResultAggregator();
        }

        [Test]
        public void AggregateScoreDetails_MatchCount_SumsMatchCountAtAllLoci()
        {
            var scoreDetails = new ScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchCountAtLocus(Locus.C, 2)
                .WithMatchCountAtLocus(Locus.Dpb1, 2)
                .WithMatchCountAtLocus(Locus.Dqb1, 2)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreDetails);

            aggregate.MatchCount.Should().Be(12);
        }

        [Test]
        public void AggregateScoreDetails_PotentialMatchCount_SumsMatchCountOnlyWhereMatchIsPotential()
        {
            var scoreDetails = new ScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchConfidenceAtLocus(Locus.B, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.C, 2)
                .WithMatchConfidenceAtLocus(Locus.C, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.Dpb1, 2)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, MatchConfidence.Definite)
                .WithMatchCountAtLocus(Locus.Dqb1, 2)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, MatchConfidence.Exact)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .WithMatchConfidenceAtLocus(Locus.Drb1, MatchConfidence.Mismatch)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreDetails);

            aggregate.PotentialMatchCount.Should().Be(6);
        }

        [Test]
        public void AggregateScoreDetails_GradeScore_SumsGradeScoreAtAllLoci()
        {
            var scoreDetails = new ScoreResultBuilder()
                .WithMatchGradeScoreAtLocus(Locus.A, 10)
                .WithMatchGradeScoreAtLocus(Locus.B, 10)
                .WithMatchGradeScoreAtLocus(Locus.C, 10)
                .WithMatchGradeScoreAtLocus(Locus.Dpb1, 10)
                .WithMatchGradeScoreAtLocus(Locus.Dqb1, 10)
                .WithMatchGradeScoreAtLocus(Locus.Drb1, 10)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreDetails);

            aggregate.GradeScore.Should().Be(60);
        }

        [Test]
        public void AggregateScoreDetails_ConfidenceScore_SumsConfidenceScoreAtAllLoci()
        {
            var scoreDetails = new ScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.B, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.C, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Dpb1, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Dqb1, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Drb1, 10)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreDetails);

            aggregate.ConfidenceScore.Should().Be(60);
        }

        [Test]
        public void AggregateScoreDetails_OverallMatchConfidence_WhenAllMatchConfidencesEqual_ReturnsMatchConfidence()
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

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.OverallMatchConfidence.Should().Be(matchConfidence);
        }
        
        [Test]
        public void AggregateScoreDetails_OverallMatchConfidence_WhenMatchConfidencesDifferPerLocus_ReturnsLowestMatchConfidence()
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
            
            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.OverallMatchConfidence.Should().Be(lowerMatchConfidence);
        }     
        
        [Test]
        public void AggregateScoreDetails_OverallMatchConfidence_WhenMatchConfidencesDifferPerPosition_ReturnsLowestMatchConfidence()
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

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.OverallMatchConfidence.Should().Be(lowerMatchConfidence);
        }
        
        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesDefinite_ReturnsDefinite()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Definite).Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Definite);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesExact_ReturnsExact()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Exact).Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Exact);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenOneLocusConfidenceIsExact_ReturnsExact()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Exact)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Exact);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesPotential_ReturnsPotential()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Potential).Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenOneLocusConfidenceIsPotential_ReturnsPotential()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Potential)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesMismatch_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Mismatch).Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenOneLocusConfidenceIsMismatch_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Mismatch)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithOnePermissiveMismatchAtDpb1_ReturnsPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchGrade.PermissiveMismatch)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithTwoPermissiveMismatchesAtDpb1_ReturnsPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocus(Locus.Dpb1, MatchGrade.PermissiveMismatch)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithOnePermissiveMismatchAndOneNonPermissiveMismatchAtDpb1_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchGrade.PermissiveMismatch)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, TypePosition.Two, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, TypePosition.Two, MatchGrade.Mismatch)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(scoreResult);

            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }
    }
}