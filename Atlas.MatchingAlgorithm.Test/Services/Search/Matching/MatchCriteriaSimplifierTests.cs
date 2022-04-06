using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    [TestFixture]
    public class MatchCriteriaSimplifierTests
    {
        [Test]
        public void SplitSearch_WithNoMismatches_ReturnsOriginalSearch()
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(0)
                .WithRequiredLociMatchCriteria(0)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(1);
            splitSearch.Single().Should().BeEquivalentTo(search);
        }
        
        [TestCase(1, 0, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 0, 1)]
        [TestCase(1, 1, 0)]
        [TestCase(1, 0, 1)]
        [TestCase(0, 1, 1)]
        public void SplitSearch_WithOneMismatch_ExcludingAtLeastOneRequiredLocus_ReturnsOriginalSearch(int a, int b, int drb1)
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMismatchCount(Locus.A, a)
                .WithLocusMismatchCount(Locus.B, b)
                .WithLocusMismatchCount(Locus.Drb1, drb1)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(1);
            splitSearch.Single().Should().BeEquivalentTo(search);
        }
        
        [Test]
        public void SplitSearch_WithOneMismatch_IncludingAllRequiredLoci_SplitsSearch()
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithRequiredLociMatchCriteria(1)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(2);
            splitSearch.Count(s => MatchesMismatchCounts(s, 1, 1, 1, 0)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 1, 0, 0, 1)).Should().Be(1);
        }
        
        [Test]
        public void SplitSearch_WithOneMismatch_IncludingAllRequiredAndOptionalLoci_SplitsSearch()
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithRequiredLociMatchCriteria(1)
                .WithLocusMismatchCount(Locus.C, 1)
                .WithLocusMismatchCount(Locus.Dqb1, 1)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(2);
            splitSearch.Count(s => MatchesMismatchCounts(s, 1, 1, 1, 0, 1, 1)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 1, 0, 0, 1, 0, 0)).Should().Be(1);
        }
        
        [TestCase(2, 0, 0)]
        [TestCase(0, 2, 0)]
        [TestCase(0, 0, 2)]
        [TestCase(1, 1, 0)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 0, 1)]
        [TestCase(2, 1, 0)]
        [TestCase(2, 2, 0)]
        [TestCase(2, 0, 1)]
        [TestCase(2, 0, 2)]
        [TestCase(1, 2, 0)]
        [TestCase(2, 2, 0)]
        [TestCase(0, 2, 1)]
        [TestCase(0, 2, 2)]
        [TestCase(0, 1, 2)]
        [TestCase(0, 2, 2)]
        [TestCase(1, 0, 2)]
        [TestCase(2, 0, 2)]
        public void SplitSearch_WithTwoMismatches_ExcludingAtLeastOneRequiredLocus_ReturnsOriginalSearch(int a, int b, int drb1)
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMismatchCount(Locus.A, a)
                .WithLocusMismatchCount(Locus.B, b)
                .WithLocusMismatchCount(Locus.Drb1, drb1)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(1);
            splitSearch.Single().Should().BeEquivalentTo(search);
        }
        
        [Test]
        public void SplitSearch_WithTwoMismatches_IncludingTwoAtAllRequiredLoci_SplitsSearch()
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithRequiredLociMatchCriteria(2)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(5);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 2, 2, 0)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 0, 0, 2)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 1, 1, 0)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 1, 0, 1)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 0, 1, 1)).Should().Be(1);
        }
        
        [Test]
        public void SplitSearch_WithTwoMismatches_IncludingTwoAtAllRequiredAndOptionalLoci_SplitsSearch()
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithRequiredLociMatchCriteria(2)
                .WithLocusMismatchCount(Locus.C, 2)
                .WithLocusMismatchCount(Locus.Dqb1, 2)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(5);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 2, 2, 0, 2, 2)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 0, 0, 2, 0, 0)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 1, 1, 0, 1, 1)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 1, 0, 1, 1, 1)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 2, 0, 1, 1, 1, 1)).Should().Be(1);
        }
        
        [Test]
        public void SplitSearch_WithThreeMismatches_IncludingTwoAtAllRequiredAndOptionalLoci_SplitsSearch()
        {
            var search = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithRequiredLociMatchCriteria(2)
                .WithLocusMismatchCount(Locus.C, 2)
                .WithLocusMismatchCount(Locus.Dqb1, 2)
                .Build();

            var splitSearch = MatchCriteriaSimplifier.SplitSearch(search);

            splitSearch.Count.Should().Be(4);
            splitSearch.Count(s => MatchesMismatchCounts(s, 3, 2, 2, 0, 2, 2)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 3, 2, 0, 2, 2, 2)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 3, 0, 2, 2, 2, 2)).Should().Be(1);
            splitSearch.Count(s => MatchesMismatchCounts(s, 3, 1, 1, 1, 2, 2)).Should().Be(1);
        }
        
        private static bool MatchesMismatchCounts(AlleleLevelMatchCriteria criteria, int total, int a, int b, int drb1, int? c = null, int? dqb1 = null)
        {
            var requiredDataMatches = criteria.DonorMismatchCount == total
                   && criteria.LocusCriteria.A.MismatchCount == a
                   && criteria.LocusCriteria.B.MismatchCount == b
                   && criteria.LocusCriteria.Drb1.MismatchCount == drb1;

            var cMatches = c == null || criteria.LocusCriteria.C.MismatchCount == c.Value;
            var dqb1Matches = dqb1 == null || criteria.LocusCriteria.Dqb1.MismatchCount == dqb1.Value;

            return requiredDataMatches && cMatches && dqb1Matches;
        }
    }
}