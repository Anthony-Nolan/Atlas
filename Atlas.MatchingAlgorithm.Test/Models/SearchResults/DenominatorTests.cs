using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using FluentAssertions;
using LochNessBuilder;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Models.SearchResults
{
    [TestFixture]
    internal class DenominatorTests
    {
        /// <summary>
        /// Using <see cref="OriginalMatchingAlgorithmResultSet"/> as concrete representation of underlying base class <see cref="ResultSet{TResult}"/>
        /// to test base functionality of <see cref="ResultSet{TResult}.MatchCriteriaDenominator"/> and <see cref="ResultSet{TResult}.ScoringCriteriaDenominator"/>
        /// </summary>
        private static Builder<OriginalMatchingAlgorithmResultSet> ResultBuilder => Builder<OriginalMatchingAlgorithmResultSet>.New;

        [Test]
        public void MatchCriteriaDenominator_MatchAllLoci_Returns10([Range(0, 2)] int mismatchCount)
        {
            var request = new SearchRequestBuilder().WithMismatchCountAtLoci(new[] { Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1 }, mismatchCount).Build();
            var result = ResultBuilder.With(x => x.SearchRequest, request).Build();

            result.MatchCriteriaDenominator.Should().Be(10);
        }

        [Test]
        public void MatchCriteriaDenominator_MatchRequiredLociOnly_Returns6([Range(0, 2)] int mismatchCount)
        {
            var request = new SearchRequestBuilder().WithMismatchCountAtLoci(new[] { Locus.A, Locus.B, Locus.Drb1 }, mismatchCount).Build();
            var result = ResultBuilder.With(x => x.SearchRequest, request).Build();

            result.MatchCriteriaDenominator.Should().Be(6);
        }

        [Test]
        public void ScoringCriteriaDenominator_ScoreAllLoci_Returns12()
        {
            var request = new SearchRequestBuilder().WithLociToScore(new[] { Locus.A, Locus.B, Locus.C, Locus.Dpb1, Locus.Dqb1, Locus.Drb1 }).Build();
            var result = ResultBuilder.With(x => x.SearchRequest, request).Build();

            result.ScoringCriteriaDenominator.Should().Be(12);
        }

        [Test]
        public void ScoringCriteriaDenominator_ScoreAllLoci_ExcludeDpb1FromAggregation_Returns10()
        {
            var request = new SearchRequestBuilder()
                .WithLociToScore(new[] { Locus.A, Locus.B, Locus.C, Locus.Dpb1, Locus.Dqb1, Locus.Drb1 })
                .WithLociExcludedFromScoringAggregates(new[] { Locus.Dpb1 })
                .Build();
            var result = ResultBuilder.With(x => x.SearchRequest, request).Build();

            result.ScoringCriteriaDenominator.Should().Be(10);
        }

        /// <summary>
        /// It's possible that a consumer will only use the scoring feature for DPB1.
        /// This test documents that the scoring denominator will only be `2` in this use case.
        /// </summary>
        [Test]
        public void ScoringCriteriaDenominator_OnlyScoreDpb1_Returns2()
        {
            var request = new SearchRequestBuilder().WithLociToScore(new[] { Locus.Dpb1 }).Build();
            var result = ResultBuilder.With(x => x.SearchRequest, request).Build();

            result.ScoringCriteriaDenominator.Should().Be(2);
        }

        [Test]
        public void ScoringCriteriaDenominator_LociToScoreIsEmpty_ReturnsNull()
        {
            var request = new SearchRequestBuilder().Build();
            var result = ResultBuilder.With(x => x.SearchRequest, request).Build();

            result.ScoringCriteriaDenominator.Should().BeNull();
        }
    }
}
