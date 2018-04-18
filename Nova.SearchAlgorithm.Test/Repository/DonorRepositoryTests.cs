using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Test.Builders;
using NSubstitute;
using NUnit.Framework;

/// <summary>
/// Tests the interface between the BlobDonorMatchRepository (the blob storage implementation of a donor match repo)
/// and the DonorBlobRepository (the repo for persisting data in cloud storage)
/// </summary>
namespace Nova.SearchAlgorithm.Test.Service
{
    [TestFixture]
    public class DonorRepositoryTests : TestBase<BlobDonorMatchRepository>
    {
        private const string PGroupA1 = "p1";
        private const string PGroupA1_alternative = "p1a";
        private const string PGroupA2 = "p2";

        private readonly SearchableDonor exactMatch = new SearchableDonor { DonorId = 1 };
        private readonly SearchableDonor bothPositionsMatchGroupOne = new SearchableDonor { DonorId = 2 };
        private readonly SearchableDonor bothGroupsMatchPositionOne = new SearchableDonor { DonorId = 3 };

        private IDonorMatchRepository repositoryUnderTest;

        [SetUp]
        public void SetUp()
        {
            repositoryUnderTest = new BlobDonorMatchRepository(GetFake<IDonorBlobRepository>());

            GetFake<IDonorBlobRepository>().GetDonorMatchesAtLocus(SearchType.Adult, Arg.Any<IEnumerable<RegistryCode>>(), "A", Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor("A", TypePositions.One, TypePositions.One, exactMatch, PGroupA1),
                HlaMatchFor("A", TypePositions.Two, TypePositions.Two, exactMatch, PGroupA2),

                HlaMatchFor("A", TypePositions.One, TypePositions.One, bothPositionsMatchGroupOne, PGroupA1),
                HlaMatchFor("A", TypePositions.One, TypePositions.Two, bothPositionsMatchGroupOne, PGroupA1),

                HlaMatchFor("A", TypePositions.One, TypePositions.One, bothGroupsMatchPositionOne, PGroupA1),
                HlaMatchFor("A", TypePositions.Two, TypePositions.One, bothGroupsMatchPositionOne, PGroupA2),
            });

            GetFake<IDonorBlobRepository>().GetDonor(exactMatch.DonorId).Returns(exactMatch);
            GetFake<IDonorBlobRepository>().GetDonor(bothPositionsMatchGroupOne.DonorId).Returns(bothPositionsMatchGroupOne);
            GetFake<IDonorBlobRepository>().GetDonor(bothGroupsMatchPositionOne.DonorId).Returns(bothGroupsMatchPositionOne);
        }

        private PotentialHlaMatchRelation HlaMatchFor(string locus, TypePositions searchPosition, TypePositions matchPosition, SearchableDonor donor, string hlaMatchName)
        {
            return new PotentialHlaMatchRelation
            {
                DonorId = donor.DonorId,
                SearchTypePosition = searchPosition,
                MatchingTypePositions = matchPosition,
                Locus = locus,
                Name = hlaMatchName
            };
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenTwoMatchesRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == exactMatch.DonorId);
            results.Where(d => d.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(2);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().NotContain(d => d.DonorId == bothPositionsMatchGroupOne.DonorId);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().NotContain(d => d.DonorId == bothGroupsMatchPositionOne.DonorId);
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenOneMatchRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == exactMatch.DonorId);
            results.Where(d => d.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(2);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenOneMatchRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenOneMatchRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenNoMatchRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == exactMatch.DonorId);
            results.Where(d => d.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(2);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenNoMatchRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenNoMatchRequired()
        {
            DonorMatchCriteria criteria = new DonorMatchCriteriaBuilder().WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            IEnumerable<PotentialMatch> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }
    }
}