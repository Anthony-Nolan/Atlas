using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Matching;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Builders;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Services.Matching
{
    [TestFixture]
    public class DatabaseDonorMatchingServiceTests : TestBase<DatabaseDonorMatchingService>
    {
        private const string PGroupA1 = "p1";
        private const string PGroupA2 = "p2";
        private const string PGroupB = "14";
        private const string PGroupDrb1 = "pgDRB1";

        private readonly DonorResult donor_ExactMatch_AtLocusA =
            new DonorResult {DonorId = 1, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorResult donor_BothPositionsMatchPatientPositionOne_AtLocusA =
            new DonorResult {DonorId = 2, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorResult donor_OnePositionMatchesBothPatientPositions_AtLocusA =
            new DonorResult {DonorId = 3, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorResult donor_NoMatch_AtLocusA =
            new DonorResult {DonorId = 4, MatchingHla = new PhenotypeInfo<ExpandedHla>(), HlaNames = new PhenotypeInfo<string>()};

        private IDatabaseDonorMatchingService donorMatchingService;

        private DonorMatchCriteriaBuilder criteriaBuilder;
        private readonly List<Locus> loci = new List<Locus> {Locus.A, Locus.B, Locus.Drb1};

        [SetUp]
        public void SetUp()
        {
            var donorSearchRepository = GetFake<IDonorSearchRepository>();
            var matchFilteringService = new MatchFilteringService();
            var databaseFilteringAnalyser = Substitute.For<IDatabaseFilteringAnalyser>();
            donorMatchingService = new DatabaseDonorMatchingService(donorSearchRepository, matchFilteringService, databaseFilteringAnalyser);

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.A, Arg.Any<LocusSearchCriteria>(), Arg.Any<MatchingFilteringOptions>())
                .Returns(new List<PotentialHlaMatchRelation>
                {
                    HlaMatchFor(Locus.A, TypePositions.One, TypePositions.One, donor_ExactMatch_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, TypePositions.Two, TypePositions.Two, donor_ExactMatch_AtLocusA, PGroupA2),
                    HlaMatchFor(Locus.A, TypePositions.One, TypePositions.Both, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, TypePositions.One, TypePositions.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, TypePositions.Two, TypePositions.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupA2),
                });

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.B, Arg.Any<LocusSearchCriteria>(), Arg.Any<MatchingFilteringOptions>())
                .Returns(new List<PotentialHlaMatchRelation>
                {
                    HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, donor_ExactMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, donor_ExactMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.One, TypePositions.Both, donor_NoMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, TypePositions.Two, TypePositions.Both, donor_NoMatch_AtLocusA, PGroupB),
                });

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.Drb1, Arg.Any<LocusSearchCriteria>(), Arg.Any<MatchingFilteringOptions>())
                .Returns(new List<PotentialHlaMatchRelation>
                {
                    HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, donor_ExactMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, donor_ExactMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.One, TypePositions.Both, donor_NoMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, TypePositions.Two, TypePositions.Both, donor_NoMatch_AtLocusA, PGroupDrb1),
                });

            criteriaBuilder = new DonorMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchB(PGroupB, PGroupB, 2)
                .WithLocusMismatchDRB1(PGroupDrb1, PGroupDrb1, 2);
        }

        private static PotentialHlaMatchRelation HlaMatchFor(Locus locus, TypePositions searchPosition, TypePositions matchPosition,
            DonorResult donor, string hlaMatchName)
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
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_ReturnsExactMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId).TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_DoesNotReturnSingleMatchDonorOnSearchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_DoesNotReturnSingleMatchDonorOnMatchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_DoesNotReturnDonorWithNoMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.DonorId == donor_NoMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenMatchesRequiredAtMultipleLoci_DoesNotReturnDonorWithMatchAtOnlyOneLocus()
        {
            var criteria = criteriaBuilder
                .WithLocusMismatchA(PGroupA1, PGroupA2, 0)
                .WithLocusMismatchB(PGroupB, PGroupB, 0)
                .Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.DonorId == donor_NoMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenMatchesRequiredAtMultipleLoci_ReturnDonorWithMatchAtAllLoci()
        {
            var criteria = criteriaBuilder
                .WithLocusMismatchA(PGroupA1, PGroupA2, 0)
                .WithLocusMismatchB(PGroupB, PGroupB, 0)
                .WithLocusMismatchDRB1(PGroupDrb1, PGroupDrb1, 0)
                .Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_ReturnsExactMatchDonor()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId).MatchDetailsForLocus(Locus.A).MatchCount.Should().Be(2);
            results.Single(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId).TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_ReturnsSingleMatchDonorOnSearchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_ReturnsSingleMatchDonorOnMatchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_DoesNotReturnDonroWithNoMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.DonorId == donor_NoMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsExactMatchDonor()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId).MatchDetailsForLocus(Locus.A).MatchCount.Should().Be(2);
            results.Single(d => d.DonorId == donor_ExactMatch_AtLocusA.DonorId).TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsSingleMatchDonorOnSearchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.DonorId == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsSingleMatchDonorOnMatchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId);
            results.Single(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.DonorId == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsDonorWithNoMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await donorMatchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.DonorId == donor_NoMatch_AtLocusA.DonorId);
        }
    }
}