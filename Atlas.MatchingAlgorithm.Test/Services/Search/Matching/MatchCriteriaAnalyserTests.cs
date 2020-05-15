using System.Linq;
using Atlas.Common.GeneticData;
using FluentAssertions;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Matching;
using Atlas.MatchingAlgorithm.Test.Builders;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Matching
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
        public void LociToMatchInDatabase_ForExactSearch_ReturnsBAndDrb1()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 0)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.B, Locus.Drb1});
        }

        // The logic in this test especially is based on an unproved hypothesis, and is likely to change based on performance investigations
        [Test]
        public void LociToMatchInDatabase_WithSingleAllowedMismatchesAtBAndDrb1_ReturnsA()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 1)
                .WithLocusMismatchDRB1("", "", 1)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria).ToList();

            lociToMatchInDatabase.Should().Contain(Locus.A);
            lociToMatchInDatabase.Count().Should().Be(2);
        }
        
        [Test]
        public void LociToMatchInDatabase_WithSingleAllowedMismatchAtA_ReturnsBAndDrb1()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 1)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 0)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.B, Locus.Drb1});
        }
        
        [Test]
        public void LociToMatchInDatabase_WithSingleAllowedMismatchAtB_ReturnsAAndDrb1()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 1)
                .WithLocusMismatchDRB1("", "", 0)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.A, Locus.Drb1});
        }
        
        [Test]
        public void LociToMatchInDatabase_WithSingleAllowedMismatchAtDrb1_ReturnsAAndB()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 1)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.A, Locus.B});
        }

        [Test]
        public void LociToMatchInDatabase_WithTwoAllowedMismatchesAtB_DoesNotIncludeB()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 2)
                .WithLocusMismatchDRB1("", "", 0)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().NotContain(Locus.B);
        }

        [Test]
        public void LociToMatchInDatabase_WithTwoAllowedMismatchesAtDrb1_DoesNotIncludeDrb1()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 2)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().NotContain(Locus.Drb1);
        }

        [Test]
        public void LociToMatchInDatabase_WithTwoAllowedMismatchesAtDrb1_IncludesA()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 2)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().Contain(Locus.A);
        }

        [Test]
        public void LociToMatchInDatabase_WithTwoAllowedMismatchesAtB_IncludesA()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 0)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 2)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().Contain(Locus.A);
        }

        [Test]
        public void LociToMatchInDatabase_WithTwoAllowedMismatchesAtBAndA_ReturnsDrb1Only()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 2)
                .WithLocusMismatchB("", "", 2)
                .WithLocusMismatchDRB1("", "", 0)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.Drb1});
        }

        [Test]
        public void LociToMatchInDatabase_WithTwoAllowedMismatchesAtDrb1AndA_ReturnsBOnly()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithLocusMismatchA("", "", 2)
                .WithLocusMismatchB("", "", 0)
                .WithLocusMismatchDRB1("", "", 2)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.B});
        }

        [Test]
        public void LociToMatchInDatabase_WithFourAllowedMismatches_AcrossAllRequiredLoci_ReturnsABAndDrb1()
        {
            var criteria = new DonorMatchCriteriaBuilder()
                .WithDonorMismatchCount(4)
                .WithLocusMismatchA("", "", 2)
                .WithLocusMismatchB("", "", 2)
                .WithLocusMismatchDRB1("", "", 2)
                .Build();

            var lociToMatchInDatabase = criteriaAnalyser.LociToMatchFirst(criteria);

            lociToMatchInDatabase.Should().BeEquivalentTo(new[] {Locus.A, Locus.B, Locus.Drb1});
        }
    }
}