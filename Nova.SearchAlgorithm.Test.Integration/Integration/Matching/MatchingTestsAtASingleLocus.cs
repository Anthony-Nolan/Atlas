using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.Integration.Builders;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration.Matching
{
    [TestFixture(DonorStorageImplementation.SQL, Locus.A)]
    [TestFixture(DonorStorageImplementation.SQL, Locus.B)]
    [TestFixture(DonorStorageImplementation.SQL, Locus.Drb1)]
    [TestFixture(DonorStorageImplementation.SQL, Locus.Dqb1)]
    [TestFixture(DonorStorageImplementation.SQL, Locus.C)]
    public class MatchingTestsAtASingleLocus : IntegrationTestBase
    {
        private AlleleLevelMatchCriteriaBuilder defaultSearchCriteriaBuilder;

        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocus;
        private InputDonor donorWithFullExactHeterozygousMatchAtLocus;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus;
        private InputDonor donorWithNoMatchAtLocus;
        private InputDonor donorWithFullCrossHeterozygousMatchAtLocus;

        private readonly List<string> matchingPGroups = new List<string> {"dummy-matching-p-group"};
        private readonly Locus locus;
        private readonly List<string> patientPGroupsAtPositionOne = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne};
        private readonly List<string> patientPGroupsAtPositionTwo = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo};

        private const string PatientPGroupAtBothPositions = "patient-p-group-at-both-positions";
        private const string PatientPGroupAtPositionOne = "patient-p-group-at-position-one";
        private const string PatientPGroupAtPositionTwo = "patient-p-group-at-position-two";

        public MatchingTestsAtASingleLocus(DonorStorageImplementation param, Locus locus) : base(param)
        {
            this.locus = locus;
        }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = container.Resolve<IDonorMatchingService>();
        }

        /// <summary>
        /// Set up test data. This test suite is only testing variations on Locus A - the other required loci should be assumed to always match
        /// </summary>
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = container.Resolve<IDonorImportRepository>();

            donorWithFullHomozygousMatchAtLocus = new InputDonorBuilder(1)
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, "non-matching-pgroup"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .Build();
            importRepo.AddOrUpdateDonorWithHla(donorWithFullHomozygousMatchAtLocus).Wait();

            donorWithFullExactHeterozygousMatchAtLocus = new InputDonorBuilder(2)
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionTwo, "non-matching-pgroup"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .Build();
            importRepo.AddOrUpdateDonorWithHla(donorWithFullExactHeterozygousMatchAtLocus).Wait();

            donorWithFullCrossHeterozygousMatchAtLocus = new InputDonorBuilder(3)
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionTwo}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .Build();
            importRepo.AddOrUpdateDonorWithHla(donorWithFullCrossHeterozygousMatchAtLocus).Wait();


            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus = new InputDonorBuilder(4)
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .Build();

            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus).Wait();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus = new InputDonorBuilder(5)
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo}},
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .Build();
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus).Wait();

            donorWithNoMatchAtLocus = new InputDonorBuilder(6)
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}},
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .Build();
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocus).Wait();
        }

        [SetUp]
        public void ResetSearchCriteria()
        {
            defaultSearchCriteriaBuilder = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo
                })
                .WithDefaultLocusMatchCriteria(new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = matchingPGroups,
                    PGroupsToMatchInPositionTwo = matchingPGroups
                })
                .WithTotalMismatchCount(0);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocus()
        {
            var results = await matchingService.Search(defaultSearchCriteriaBuilder.Build());
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocus()
        {
            var results = await matchingService.Search(defaultSearchCriteriaBuilder.Build());
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocus()
        {
            var results = await matchingService.Search(defaultSearchCriteriaBuilder.Build());
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorsWithTwoMatchesAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorWithOneOfTwoHvGAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorWithOneOfTwoBothDirectionsAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_DoesNotMatchDonorsWithNoMatchAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_MatchesDonorWithNoMatchAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(2)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 2,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_MatchesDonorsWithExactMatchAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(2)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 2,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_MatchesDonorsWithSingleMatchAtLocus()
        {
            var criteria = defaultSearchCriteriaBuilder
                .WithTotalMismatchCount(2)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 2,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.Search(criteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
        }
    }
}