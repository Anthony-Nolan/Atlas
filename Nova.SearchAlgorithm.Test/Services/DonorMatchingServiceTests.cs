using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Builders;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Repositories
{
    // TODO: The tests as written are just for the three loci search. Now that the five loci part is in the same service layer, the tests will fail with five loci enabled.
    [TestFixture]
    public class DonorMatchingServiceTests : TestBase<DonorMatchingService>
    {
        private const string PGroupA1 = "p1";
        private const string PGroupA1_alternative = "p1a";
        private const string PGroupA2 = "p2";
        private const string PGroupB = "14";
        private const string PGroupDRB1 = "pgDRB1";

        private readonly DonorResult exactMatch =
            new DonorResult {DonorId = 1, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorResult bothPositionsMatchGroupOne =
            new DonorResult {DonorId = 2, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorResult bothGroupsMatchPositionOne =
            new DonorResult {DonorId = 3, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private IDonorMatchingService matchingService;

        private DonorMatchCriteriaBuilder criteriaBuilder;

        [SetUp]
        public void SetUp()
        {
            var donorSearchRepository = GetFake<IDonorSearchRepository>();
            var donorInspectionRepository = GetFake<IDonorInspectionRepository>();
            var lookupService = GetFake<IMatchingDictionaryLookupService>();
            matchingService = new DonorMatchingService(donorSearchRepository, donorInspectionRepository, lookupService);

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.A, Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor(Locus.A, TypePositions.One, TypePositions.One, exactMatch, PGroupA1),
                HlaMatchFor(Locus.A, TypePositions.Two, TypePositions.Two, exactMatch, PGroupA2),

                HlaMatchFor(Locus.A, TypePositions.One, TypePositions.Both, bothPositionsMatchGroupOne, PGroupA1),

                HlaMatchFor(Locus.A, TypePositions.One, TypePositions.One, bothGroupsMatchPositionOne, PGroupA1),
                HlaMatchFor(Locus.A, TypePositions.Two, TypePositions.One, bothGroupsMatchPositionOne, PGroupA2),
            });

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.B, Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, exactMatch, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, exactMatch, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, bothPositionsMatchGroupOne, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, bothPositionsMatchGroupOne, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, bothGroupsMatchPositionOne, PGroupB),
                HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, bothGroupsMatchPositionOne, PGroupB),
            });

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.Drb1, Arg.Any<LocusSearchCriteria>()).Returns(new List<PotentialHlaMatchRelation>
            {
                HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, exactMatch, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, exactMatch, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, bothPositionsMatchGroupOne, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, bothPositionsMatchGroupOne, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, bothGroupsMatchPositionOne, PGroupDRB1),
                HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, bothGroupsMatchPositionOne, PGroupDRB1),
            });

            donorInspectionRepository.GetDonor(exactMatch.DonorId).Returns(exactMatch);
            donorInspectionRepository.GetDonor(bothPositionsMatchGroupOne.DonorId).Returns(bothPositionsMatchGroupOne);
            donorInspectionRepository.GetDonor(bothGroupsMatchPositionOne.DonorId).Returns(bothGroupsMatchPositionOne);

            criteriaBuilder = new DonorMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchB(PGroupB, PGroupB, 2)
                .WithLocusMismatchDRB1(PGroupDRB1, PGroupDRB1, 2);
        }

        private PotentialHlaMatchRelation HlaMatchFor(Locus locus, TypePositions searchPosition, TypePositions matchPosition, DonorResult donor,
            string hlaMatchName)
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

        private async Task<List<PotentialSearchResult>> Search(AlleleLevelMatchCriteria criteria)
        {
            var results = await matchingService.Search(criteria);
            return results.ToList();
        }

        [Test]
        public async Task ExactMatchDonorIsReturnedWhenTwoMatchesRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == exactMatch.DonorId);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task SingleMatchDonorOnSearchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = await Search(criteria);

            results.Should().NotContain(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId);
        }

        [Test]
        public async Task SingleMatchDonorOnMatchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = await Search(criteria);

            results.Should().NotContain(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId);
        }

        [Test]
        public async Task ExactMatchDonorIsReturnedWhenOneMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == exactMatch.DonorId);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(2);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task SingleMatchDonorOnSearchSideIsReturnedWhenOneMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task SingleMatchDonorOnMatchSideIsReturnedWhenOneMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task ExactMatchDonorIsReturnedWhenNoMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == exactMatch.DonorId);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(2);
            results.Where(d => d.Donor.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task SingleMatchDonorOnSearchSideIsReturnedWhenNoMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task SingleMatchDonorOnMatchSideIsReturnedWhenNoMatchRequired()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = await Search(criteria);

            results.Should().Contain(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().MatchDetailsAtLocusA.MatchCount.Should().Be(1);
            results.Where(d => d.Donor.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(5);
        }
    }
}