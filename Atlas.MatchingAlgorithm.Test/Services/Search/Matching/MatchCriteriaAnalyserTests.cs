using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    [TestFixture]
    public class MatchCriteriaAnalyserTests
    {
        private IMatchCriteriaAnalyser criteriaAnalyser;

        [SetUp]
        public void SetUp()
        {
            criteriaAnalyser = new MatchCriteriaAnalyser();
        }

        [Test]
        public void LociInMatchingOrder_ForExactSearch_ReturnsBAndDrb1BeforeA()
        {
            var criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(0).WithLocusMismatchB(0).WithLocusMismatchDrb1(0).Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.A);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.A);
        }

        // The logic in this test especially is based on an unproved hypothesis, and is likely to change based on performance investigations
        [Test]
        public void LociInMatchingOrder_WithSingleAllowedMismatchesAtBAndDrb1_ReturnsAFirst()
        {
            var criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(0).WithLocusMismatchB(1).WithLocusMismatchDrb1(1).Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.B);
            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.Drb1);
        }

        [Test]
        public void LociInMatchingOrder_WithSingleAllowedMismatchAtA_ReturnsBAndDrb1BeforeA()
        {
            var criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(1).WithLocusMismatchB(0).WithLocusMismatchDrb1(0).Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.A);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.A);
        }

        [Test]
        public void LociInMatchingOrder_WithSingleAllowedMismatchAtB_ReturnsAAndDrb1BeforeB()
        {
            var criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(0).WithLocusMismatchB(1).WithLocusMismatchDrb1(0).Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.B);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.B);
        }

        [Test]
        public void LociInMatchingOrder_WithSingleAllowedMismatchAtDrb1_ReturnsAAndBBeforeDrb1()
        {
            var criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(0).WithLocusMismatchB(0).WithLocusMismatchDrb1(1).Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.Drb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.Drb1);
        }

        [Test]
        public void LociInMatchingOrder_WithNoAllowedMismatches_ReturnsRequiredLociBeforeOptional()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA(0).WithLocusMismatchB(0).WithLocusMismatchDrb1(1)
                .WithLocusMismatchCount(Locus.Dqb1, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.Dqb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.C);
            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.Dqb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.C);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.Dqb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.C);
        }

        [Test]
        public void LociInMatchingOrder_WithManyAllowedMismatches_ReturnsRequiredLociBeforeOptional()
        {
            var criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(2).WithLocusMismatchB(2).WithLocusMismatchDrb1(2)
                .WithLocusMismatchCount(Locus.Dqb1, 0)
                .WithLocusMismatchCount(Locus.C, 0)
                .Build();

            var lociInMatchingOrder = criteriaAnalyser.LociInMatchingOrder(criteria);

            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.Dqb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.A, Locus.C);
            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.Dqb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.B, Locus.C);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.Dqb1);
            lociInMatchingOrder.Should().ContainInOrder(Locus.Drb1, Locus.C);
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public void LociInMatchingOrder_WhenLociExcludedFromCriteria_DoesNotReturnExcludedLoci(Locus[] excludedLoci)
        {
            var criteriaBuilder = new DonorMatchCriteriaBuilder();
            criteriaBuilder = excludedLoci.Aggregate(criteriaBuilder, (current, locus) => current.WithNoCriteriaAtLocus(locus));

            var loci = criteriaAnalyser.LociInMatchingOrder(criteriaBuilder.Build());

            loci.Should().NotContain(excludedLoci);
        }
    }
}