using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Services.Search.Scoring.Categorisation;
using Nova.SearchAlgorithm.Test.Builders.SearchResults;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Search.Scoring.Categorisation
{
    [TestFixture]
    public class MatchCategoriserTests
    {
        [Test]
        public void CategoriseMatch_WhenAllLociConfidencesDefinite_ReturnsDefinite()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Definite).Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Definite);
        }

        [Test]
        public void CategoriseMatch_WhenAllLociConfidencesExact_ReturnsExact()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Exact).Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Exact);
        }

        [Test]
        public void CategoriseMatch_WhenOneLocusConfidenceIsExact_ReturnsExact()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Exact)
                .Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Exact);
        }

        [Test]
        public void CategoriseMatch_WhenAllLociConfidencesPotential_ReturnsPotential()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Potential).Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void CategoriseMatch_WhenOneLocusConfidenceIsPotential_ReturnsPotential()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Potential)
                .Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Potential);
        }

        [Test]
        public void CategoriseMatch_WhenAllLociConfidencesMismatch_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder().WithMatchConfidenceAtAllLoci(MatchConfidence.Mismatch).Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void CategoriseMatch_WhenOneLocusConfidenceIsMismatch_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.A, MatchConfidence.Mismatch)
                .Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void CategoriseMatch_WithOnePermissiveMismatchAtDpb1_ReturnsPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchGrade.PermissiveMismatch)
                .Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void CategoriseMatch_WithTwoPermissiveMismatchesAtDpb1_ReturnsPermissiveMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocus(Locus.Dpb1, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocus(Locus.Dpb1, MatchGrade.PermissiveMismatch)
                .Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Mismatch);
        }

        [Test]
        public void CategoriseMatch_WithOnePermissiveMismatchAndOneNonPermissiveMismatchAtDpb1_ReturnsMismatch()
        {
            var scoreResult = new ScoreResultBuilder()
                .WithMatchConfidenceAtAllLoci(MatchConfidence.Definite)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, TypePosition.One, MatchGrade.PermissiveMismatch)
                .WithMatchConfidenceAtLocusPosition(Locus.Dpb1, TypePosition.Two, MatchConfidence.Mismatch)
                .WithMatchGradeAtLocusPosition(Locus.Dpb1, TypePosition.Two, MatchGrade.Mismatch)
                .Build();

            var category = MatchCategoriser.CategoriseMatch(scoreResult);

            category.Should().Be(MatchCategory.Mismatch);
        }
    }
}