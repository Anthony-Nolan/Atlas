using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    [TestFixture(Locus.A)]
    [TestFixture(Locus.B)]
    [TestFixture(Locus.Drb1)]
    [TestFixture(Locus.Dqb1)]
    [TestFixture(Locus.C)]
    public class MatchingTestsAtASingleLocus : IntegrationTestBase
    {
        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocus;
        private InputDonor donorWithFullExactHeterozygousMatchAtLocus;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus;
        private InputDonor donorWithNoMatchAtLocus;
        private InputDonor donorWithFullCrossHeterozygousMatchAtLocus;

        private const DonorType DefaultDonorType = DonorType.Cord;
        
        private readonly List<string> matchingPGroups = new List<string> {"dummy-matching-p-group"};
        private readonly Locus locus;
        private readonly List<string> patientPGroupsAtPositionOne = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne};
        private readonly List<string> patientPGroupsAtPositionTwo = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo};

        private const string PatientPGroupAtBothPositions = "patient-p-group-at-both-positions";
        private const string PatientPGroupAtPositionOne = "patient-p-group-at-position-one";
        private const string PatientPGroupAtPositionTwo = "patient-p-group-at-position-two";

        public MatchingTestsAtASingleLocus(Locus locus) : base()
        {
            this.locus = locus;
        }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = Container.Resolve<IDonorMatchingService>();
        }

        /// <summary>
        /// Set up test data. This test suite is only testing variations on Locus A - the other required loci should be assumed to always match
        /// </summary>
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = Container.Resolve<IDonorImportRepository>();

            donorWithFullHomozygousMatchAtLocus = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, "non-matching-pgroup"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .WithDonorType(DefaultDonorType)
                .Build();

            donorWithFullExactHeterozygousMatchAtLocus = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionTwo, "non-matching-pgroup"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .WithDonorType(DefaultDonorType)
                .Build();

            donorWithFullCrossHeterozygousMatchAtLocus = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionTwo}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .WithDonorType(DefaultDonorType)
                .Build();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .WithDonorType(DefaultDonorType)
                .Build();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo}},
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .WithDonorType(DefaultDonorType)
                .Build();

            donorWithNoMatchAtLocus = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}},
                    new ExpandedHla {PGroups = new List<string> {"non-matching-pgroup", "non-matching-pgroup-2"}}
                )
                .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                .WithDonorType(DefaultDonorType)
                .Build();

            var allDonors = new List<InputDonor>
            {
                donorWithFullHomozygousMatchAtLocus,
                donorWithFullExactHeterozygousMatchAtLocus,
                donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus,
                donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus,
                donorWithNoMatchAtLocus,
                donorWithFullCrossHeterozygousMatchAtLocus
            };

            foreach (var donor in allDonors)
            {
                Task.Run(() => importRepo.AddOrUpdateDonorWithHla(donor)).Wait();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocus()
        {
            var results = await matchingService.Search(GetDefaultCriteriaBuilder().Build());
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocus()
        {
            var results = await matchingService.Search(GetDefaultCriteriaBuilder().Build());
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocus()
        {
            var results = await matchingService.Search(GetDefaultCriteriaBuilder().Build());
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorsWithTwoMatchesAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
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
            var criteria = GetDefaultCriteriaBuilder()
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
            var criteria = GetDefaultCriteriaBuilder()
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
            var criteria = GetDefaultCriteriaBuilder()
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
            var criteria = GetDefaultCriteriaBuilder()
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
            var criteria = GetDefaultCriteriaBuilder()
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
            var criteria = GetDefaultCriteriaBuilder()
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

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
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
                .WithSearchType(DefaultDonorType)
                .WithTotalMismatchCount(0);
        }
    }
}