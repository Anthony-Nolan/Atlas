using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Aggregation;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchResults;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Aggregation
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
        public void AggregateScoreDetails_MatchCount_SumsMatchCountAtAllScoredLoci()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchCountAtLocus(Locus.C, 2)
                .WithMatchCountAtLocus(Locus.Dpb1, 2)
                .WithMatchCountAtLocus(Locus.Dqb1, 2)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.MatchCount.Should().Be(12);
        }

        [Test]
        public void AggregateScoreDetails_MatchCount_ExcludingDpb1_DoesNotIncludeDpb1InAggregate()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchCountAtLocus(Locus.C, 2)
                .WithMatchCountAtLocus(Locus.Dpb1, 2)
                .WithMatchCountAtLocus(Locus.Dqb1, 2)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.MatchCount.Should().Be(10);
        }

        [Test]
        public void AggregateScoreDetails_ExcludingMultipleLoci_DoesNotIncludeAnyExcludedLocus()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchCountAtLocus(Locus.C, 2)
                .WithMatchCountAtLocus(Locus.Dpb1, 2)
                .WithMatchCountAtLocus(Locus.Dqb1, 2)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.C, Locus.Dpb1, Locus.Dqb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.MatchCount.Should().Be(6);
        }

        [Test]
        public void AggregateScoreDetails_PotentialMatchCount_SumsMatchCountOnlyWhereMatchIsPotential()
        {
            var scoreResult = new ScoreResultBuilder()
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

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.PotentialMatchCount.Should().Be(6);
        }

        [Test]
        public void AggregateScoreDetails_GradeScore_SumsGradeScoreAtAllScoredLoci()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchGradeScoreAtLocus(Locus.A, 10)
                .WithMatchGradeScoreAtLocus(Locus.B, 10)
                .WithMatchGradeScoreAtLocus(Locus.C, 10)
                .WithMatchGradeScoreAtLocus(Locus.Dpb1, 10)
                .WithMatchGradeScoreAtLocus(Locus.Dqb1, 10)
                .WithMatchGradeScoreAtLocus(Locus.Drb1, 10)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.GradeScore.Should().Be(60);
        }

        [Test]
        public void AggregateScoreDetails_GradeScore_ExcludingDpb1_DoesNotIncludeDpb1InAggregate()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchGradeScoreAtLocus(Locus.A, 10)
                .WithMatchGradeScoreAtLocus(Locus.B, 10)
                .WithMatchGradeScoreAtLocus(Locus.C, 10)
                .WithMatchGradeScoreAtLocus(Locus.Dpb1, 10)
                .WithMatchGradeScoreAtLocus(Locus.Dqb1, 10)
                .WithMatchGradeScoreAtLocus(Locus.Drb1, 10)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.GradeScore.Should().Be(50);
        }

        [Test]
        public void AggregateScoreDetails_ConfidenceScore_SumsConfidenceScoreAtAllScoredLoci()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.B, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.C, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Dpb1, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Dqb1, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Drb1, 10)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.ConfidenceScore.Should().Be(60);
        }

        [Test]
        public void AggregateScoreDetails_ConfidenceScore_ExcludingDpb1_DoesNotIncludeDpb1InAggregate()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceScoreAtLocus(Locus.A, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.B, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.C, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Dpb1, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Dqb1, 10)
                .WithMatchConfidenceScoreAtLocus(Locus.Drb1, 10)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.ConfidenceScore.Should().Be(50);
        }

        [Test]
        public void AggregateScoreDetails_TypedLociCount_CountsTypedLociAtAllScoredLoci()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithTypingAtLocus(Locus.A)
                .WithTypingAtLocus(Locus.B)
                .WithTypingAtLocus(Locus.C)
                .WithTypingAtLocus(Locus.Dpb1)
                .WithTypingAtLocus(Locus.Dqb1)
                .WithTypingAtLocus(Locus.Drb1)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);

            aggregate.TypedLociCount.Should().Be(6);
        }

        [Test]
        public void AggregateScoreDetails_TypedLociCount_ExcludingDpb1_WhenDpb1Typed_DoesNotIncludeDpb1()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithTypingAtLocus(Locus.A)
                .WithTypingAtLocus(Locus.B)
                .WithTypingAtLocus(Locus.C)
                .WithTypingAtLocus(Locus.Dpb1)
                .WithTypingAtLocus(Locus.Dqb1)
                .WithTypingAtLocus(Locus.Drb1)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.TypedLociCount.Should().Be(5);
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

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
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

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.OverallMatchConfidence.Should().Be(lowerMatchConfidence);
        }

        [Test]
        public void AggregateScoreDetails_OverallMatchConfidence_WhenMatchConfidencesDifferPerPosition_ReturnsLowestMatchConfidence()
        {
            const MatchConfidence higherMatchConfidence = MatchConfidence.Exact;
            const MatchConfidence lowerMatchConfidence = MatchConfidence.Potential;
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtLocus(Locus.A, higherMatchConfidence)
                .WithMatchConfidenceAtLocusPosition(Locus.B, LocusPosition.One, higherMatchConfidence)
                .WithMatchConfidenceAtLocusPosition(Locus.B, LocusPosition.Two, lowerMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.C, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Drb1, higherMatchConfidence)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.OverallMatchConfidence.Should().Be(lowerMatchConfidence);
        }

        [Test]
        public void AggregateScoreDetails_OverallMatchConfidence_ExcludingDpb1_ReturnsLowestMatchConfidenceOfOtherLoci()
        {
            const MatchConfidence higherMatchConfidence = MatchConfidence.Exact;
            const MatchConfidence midMatchConfidence = MatchConfidence.Potential;
            const MatchConfidence lowerMatchConfidence = MatchConfidence.Mismatch;
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtLocus(Locus.A, higherMatchConfidence)
                .WithMatchConfidenceAtLocusPosition(Locus.B, LocusPosition.One, higherMatchConfidence)
                .WithMatchConfidenceAtLocusPosition(Locus.B, LocusPosition.Two, midMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.C, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, lowerMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, higherMatchConfidence)
                .WithMatchConfidenceAtLocus(Locus.Drb1, higherMatchConfidence)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.OverallMatchConfidence.Should().Be(midMatchConfidence);
        }

        [Test]
        public void AggregateScoreDetails_PotentialMatchCount_ExcludingDpb1_DoesNotIncludeDpb1InAggregate()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchCountAtLocus(Locus.A, 2)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.B, 2)
                .WithMatchConfidenceAtLocus(Locus.B, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.C, 2)
                .WithMatchConfidenceAtLocus(Locus.C, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.Dpb1, 2)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, MatchConfidence.Potential)
                .WithMatchCountAtLocus(Locus.Dqb1, 2)
                .WithMatchConfidenceAtLocus(Locus.Dqb1, MatchConfidence.Exact)
                .WithMatchCountAtLocus(Locus.Drb1, 2)
                .WithMatchConfidenceAtLocus(Locus.Drb1, MatchConfidence.Mismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.PotentialMatchCount.Should().Be(6);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesDefinite_ReturnsDefinite()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Definite).Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Definite);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesExact_ReturnsExact()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Exact).Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Exact);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenOneLocusConfidenceIsExact_ReturnsExact()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Exact)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Exact);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesPotential_ReturnsPotential()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Potential).Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_ExcludingDpb1_WhenOnlyDpb1IsMismatch_DoesNotReturnMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Potential)
                .WithMatchGradeAtAllLoci(MatchGrade.GDna)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, MatchConfidence.Mismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenOneLocusConfidenceIsPotential_ReturnsPotential()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchGradeAtAllLoci(MatchGrade.GDna)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Potential)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenAllLociConfidencesMismatch_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Mismatch).Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WhenOneLocusConfidenceIsMismatch_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Mismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithOnePermissiveMismatchAtDpb1_ReturnsPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchGradeAtAllLoci(MatchGrade.GDna)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, LocusPosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, LocusPosition.One, MatchGrade.PermissiveMismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.PermissiveMismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithOnePermissiveMismatchAtDpb1_AndDpb1Excluded_DoesNotReturnPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchGradeAtAllLoci(MatchGrade.GDna)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, LocusPosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, LocusPosition.One, MatchGrade.PermissiveMismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .With(x => x.LociToExclude, new List<Locus> { Locus.Dpb1 })
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Definite);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithTwoPermissiveMismatchesAtDpb1_ReturnsPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchGradeAtAllLoci(MatchGrade.GDna)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocus(Locus.Dpb1, MatchGrade.PermissiveMismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.PermissiveMismatch);
        }

        [Test]
        public void AggregateScoreDetails_MatchCategory_WithOnePermissiveMismatchAndOneNonPermissiveMismatchAtDpb1_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchGradeAtAllLoci(MatchGrade.GDna)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, LocusPosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, LocusPosition.One, MatchGrade.PermissiveMismatch)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, LocusPosition.Two, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, LocusPosition.Two, MatchGrade.Mismatch)
                .Build();

            var parameters = ScoreResultAggregatorParametersBuilder.New
                .With(x => x.ScoreResult, scoreResult)
                .Build();

            var aggregate = resultAggregator.AggregateScoreDetails(parameters);
            aggregate.MatchCategory.Should().Be(MatchCategory.Mismatch);
        }
    }
}