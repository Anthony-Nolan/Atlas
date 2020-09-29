using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.Matching;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    [TestFixture]
    public class DonorMatchingServiceTests
    {
        private const string PGroupA1 = "p1";
        private const string PGroupA2 = "p2";
        private const string PGroupB = "14";
        private const string PGroupDrb1 = "pgDRB1";

        private readonly DonorInfoWithExpandedHla donor_ExactMatch_AtLocusA =
            new DonorInfoWithExpandedHla
                {DonorId = 1, MatchingHla = new PhenotypeInfo<IHlaMatchingMetadata>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorInfoWithExpandedHla donor_BothPositionsMatchPatientPositionOne_AtLocusA =
            new DonorInfoWithExpandedHla
                {DonorId = 2, MatchingHla = new PhenotypeInfo<IHlaMatchingMetadata>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorInfoWithExpandedHla donor_OnePositionMatchesBothPatientPositions_AtLocusA =
            new DonorInfoWithExpandedHla
                {DonorId = 3, MatchingHla = new PhenotypeInfo<IHlaMatchingMetadata>(), HlaNames = new PhenotypeInfo<string>()};

        private readonly DonorInfoWithExpandedHla donor_NoMatch_AtLocusA =
            new DonorInfoWithExpandedHla
                {DonorId = 4, MatchingHla = new PhenotypeInfo<IHlaMatchingMetadata>(), HlaNames = new PhenotypeInfo<string>()};

        private IDonorMatchingService matchingService;

        private DonorMatchCriteriaBuilder criteriaBuilder;
        private readonly List<Locus> loci = new List<Locus> {Locus.A, Locus.B, Locus.Drb1};

        [SetUp]
        public void SetUp()
        {
            var donorSearchRepository = Substitute.For<IDonorSearchRepository>();
            var matchFilteringService = new MatchFilteringService();
            var databaseFilteringAnalyser = Substitute.For<IDatabaseFilteringAnalyser>();
            var pGroupRepository = Substitute.For<IPGroupRepository>();
            var repositoryFactory = Substitute.For<IActiveRepositoryFactory>();

            repositoryFactory.GetDonorSearchRepository().Returns(donorSearchRepository);
            repositoryFactory.GetPGroupRepository().Returns(pGroupRepository);

            matchingService = new DonorMatchingService(
                repositoryFactory,
                matchFilteringService,
                databaseFilteringAnalyser,
                Substitute.For<IMatchingAlgorithmSearchLogger>()
            );

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.A, Arg.Any<LocusSearchCriteria>(), Arg.Any<MatchingFilteringOptions>())
                .Returns(new List<PotentialHlaMatchRelation>
                {
                    HlaMatchFor(Locus.A, LocusPosition.One, LocusPosition.One, donor_ExactMatch_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, LocusPosition.Two, LocusPosition.Two, donor_ExactMatch_AtLocusA, PGroupA2),
                    HlaMatchFor(Locus.A, LocusPosition.One, LocusPosition.One, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, LocusPosition.One, LocusPosition.Two, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, LocusPosition.One, LocusPosition.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupA1),
                    HlaMatchFor(Locus.A, LocusPosition.Two, LocusPosition.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupA2),
                }.ToAsyncEnumerable());

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.B, Arg.Any<LocusSearchCriteria>(), Arg.Any<MatchingFilteringOptions>())
                .Returns(new List<PotentialHlaMatchRelation>
                {
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.One, donor_ExactMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.Two, donor_ExactMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.One, donor_ExactMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.Two, donor_ExactMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.One, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.Two, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.One, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.Two, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.Two, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.Two, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.One, donor_NoMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.One, LocusPosition.Two, donor_NoMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.One, donor_NoMatch_AtLocusA, PGroupB),
                    HlaMatchFor(Locus.B, LocusPosition.Two, LocusPosition.Two, donor_NoMatch_AtLocusA, PGroupB),
                }.ToAsyncEnumerable());

            donorSearchRepository.GetDonorMatchesAtLocus(Locus.Drb1, Arg.Any<LocusSearchCriteria>(), Arg.Any<MatchingFilteringOptions>())
                .Returns(new List<PotentialHlaMatchRelation>
                {
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.One, donor_ExactMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.Two, donor_ExactMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.One, donor_ExactMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.Two, donor_ExactMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.One, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.Two, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.One, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.Two, donor_BothPositionsMatchPatientPositionOne_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.Two, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.One, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.Two, donor_OnePositionMatchesBothPatientPositions_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.One, donor_NoMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.One, LocusPosition.Two, donor_NoMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.One, donor_NoMatch_AtLocusA, PGroupDrb1),
                    HlaMatchFor(Locus.Drb1, LocusPosition.Two, LocusPosition.Two, donor_NoMatch_AtLocusA, PGroupDrb1),
                }.ToAsyncEnumerable());

            criteriaBuilder = new DonorMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchB(PGroupB, PGroupB, 2)
                .WithLocusMismatchDrb1(PGroupDrb1, PGroupDrb1, 2);
        }

        private static PotentialHlaMatchRelation HlaMatchFor(
            Locus locus,
            LocusPosition searchPosition,
            LocusPosition matchPosition,
            DonorInfoWithExpandedHla donor,
            string hlaMatchName)
        {
            return new PotentialHlaMatchRelation
            {
                DonorId = donor.DonorId,
                SearchTypePosition = searchPosition,
                MatchingTypePosition = matchPosition,
                Locus = locus,
                Name = hlaMatchName
            };
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_ReturnsExactMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_ExactMatch_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_ExactMatch_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_DoesNotReturnSingleMatchDonorOnSearchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_DoesNotReturnSingleMatchDonorOnMatchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenTwoMatchesRequired_DoesNotReturnDonorWithNoMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 0).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.Key == donor_NoMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenMatchesRequiredAtMultipleLoci_DoesNotReturnDonorWithMatchAtOnlyOneLocus()
        {
            var criteria = criteriaBuilder
                .WithLocusMismatchA(PGroupA1, PGroupA2, 0)
                .WithLocusMismatchB(PGroupB, PGroupB, 0)
                .Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.Key == donor_NoMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenMatchesRequiredAtMultipleLoci_ReturnDonorWithMatchAtAllLoci()
        {
            var criteria = criteriaBuilder
                .WithLocusMismatchA(PGroupA1, PGroupA2, 0)
                .WithLocusMismatchB(PGroupB, PGroupB, 0)
                .WithLocusMismatchDrb1(PGroupDrb1, PGroupDrb1, 0)
                .Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_ExactMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_ReturnsExactMatchDonor()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_ExactMatch_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_ExactMatch_AtLocusA.DonorId).Value.MatchDetailsForLocus(Locus.A).MatchCount.Should().Be(2);
            results.Single(d => d.Key == donor_ExactMatch_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_ReturnsSingleMatchDonorOnSearchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).Value.MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_ReturnsSingleMatchDonorOnMatchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).Value.MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenOneMatchRequired_DoesNotReturnDonroWithNoMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 1).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().NotContain(d => d.Key == donor_NoMatch_AtLocusA.DonorId);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsExactMatchDonor()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_ExactMatch_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_ExactMatch_AtLocusA.DonorId).Value.MatchDetailsForLocus(Locus.A).MatchCount.Should().Be(2);
            results.Single(d => d.Key == donor_ExactMatch_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsSingleMatchDonorOnSearchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).Value.MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.Key == donor_BothPositionsMatchPatientPositionOne_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsSingleMatchDonorOnMatchSide()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId);
            results.Single(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).Value.MatchDetailsForLocus(Locus.A).MatchCount
                .Should().Be(1);
            results.Single(d => d.Key == donor_OnePositionMatchesBothPatientPositions_AtLocusA.DonorId).Value.TotalMatchCount.Should().Be(5);
        }

        [Test]
        public async Task FindMatchesForLoci_WhenNoMatchRequired_ReturnsDonorWithNoMatch()
        {
            var criteria = criteriaBuilder.WithLocusMismatchA(PGroupA1, PGroupA2, 2).Build();

            var results = (await matchingService.FindMatchesForLoci(criteria, loci)).ToList();

            results.Should().Contain(d => d.Key == donor_NoMatch_AtLocusA.DonorId);
        }
    }
}