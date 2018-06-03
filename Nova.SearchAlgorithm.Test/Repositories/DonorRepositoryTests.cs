using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Test.Builders;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Repositories
{
    /// <summary>
    /// Tests the interface between the BlobDonorMatchRepository (the blob storage implementation of a donor match repo)
    /// and the DonorBlobRepository (the repo for persisting data in cloud storage)
    /// </summary>
    [TestFixture]
    public class DonorRepositoryTests : TestBase<CloudStorageDonorSearchRepository>
    {
        private const string PGroupA1 = "p1";
        private const string PGroupA1_alternative = "p1a";
        private const string PGroupA2 = "p2";
        private const string PGroupB = "14";
        private const string PGroupDRB1 = "pgDRB1";

        private readonly DonorResult exactMatch = new DonorResult { DonorId = 1 };
        private readonly DonorResult bothPositionsMatchGroupOne = new DonorResult { DonorId = 2 };
        private readonly DonorResult bothGroupsMatchPositionOne = new DonorResult { DonorId = 3 };

        private IDonorSearchRepository repositoryUnderTest;

        private DonorMatchCriteriaBuilder criteriaBuilder;

        [SetUp]
        public void SetUp()
        {
            repositoryUnderTest = new CloudStorageDonorSearchRepository(GetFake<IDonorDocumentStorage>());

            GetFake<IDonorDocumentStorage>().GetDonorMatchesAtLocus(Locus.A, Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor(Locus.A, TypePositions.One, TypePositions.One, exactMatch, PGroupA1),
                HlaMatchFor(Locus.A, TypePositions.Two, TypePositions.Two, exactMatch, PGroupA2),

                HlaMatchFor(Locus.A, TypePositions.One, TypePositions.Both, bothPositionsMatchGroupOne, PGroupA1),
                
                HlaMatchFor(Locus.A, TypePositions.One, TypePositions.One, bothGroupsMatchPositionOne, PGroupA1),
                HlaMatchFor(Locus.A, TypePositions.Two, TypePositions.One, bothGroupsMatchPositionOne, PGroupA2),
            });

            GetFake<IDonorDocumentStorage>().GetDonorMatchesAtLocus(Locus.B, Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, exactMatch, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, exactMatch, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, bothPositionsMatchGroupOne, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, bothPositionsMatchGroupOne, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, bothGroupsMatchPositionOne, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, bothGroupsMatchPositionOne, PGroupB),
            });

            GetFake<IDonorDocumentStorage>().GetDonorMatchesAtLocus(Locus.Drb1, Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, exactMatch, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, exactMatch, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, bothPositionsMatchGroupOne, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, bothPositionsMatchGroupOne, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, bothGroupsMatchPositionOne, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, bothGroupsMatchPositionOne, PGroupDRB1),
            });

            GetFake<IDonorDocumentStorage>().GetDonor(exactMatch.DonorId).Returns(exactMatch);
            GetFake<IDonorDocumentStorage>().GetDonor(bothPositionsMatchGroupOne.DonorId).Returns(bothPositionsMatchGroupOne);
            GetFake<IDonorDocumentStorage>().GetDonor(bothGroupsMatchPositionOne.DonorId).Returns(bothGroupsMatchPositionOne);

            criteriaBuilder = new DonorMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchB(PGroupB, PGroupB, 2)
                .WithLocusMismatchDRB1(PGroupDRB1, PGroupDRB1, 2);
        }

        private PotentialHlaMatchRelation HlaMatchFor(Locus locus, TypePositions searchPosition, TypePositions matchPosition, DonorResult donor, string hlaMatchName)
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
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == exactMatch.DonorId);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(6);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().NotContain(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().NotContain(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId);
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenOneMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == exactMatch.DonorId);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(2);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(6);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsReturnedWhenOneMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsReturnedWhenOneMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenNoMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == exactMatch.DonorId);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(2);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(6);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsReturnedWhenNoMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsReturnedWhenNoMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            IEnumerable<PotentialSearchResult> results = repositoryUnderTest.Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }
    }
}