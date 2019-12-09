using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.Search.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    [TestFixture(Locus.A)]
    [TestFixture(Locus.B)]
    [TestFixture(Locus.Drb1)]
    [TestFixture(Locus.Dqb1)]
    [TestFixture(Locus.C)]
    public class MatchingTestsAtASingleLocus
    {
        private IDonorMatchingService matchingService;
        private DonorInfoWithExpandedHla donorInfoWithFullHomozygousMatchAtLocus;
        private DonorInfoWithExpandedHla donorInfoWithFullExactHeterozygousMatchAtLocus;
        private DonorInfoWithExpandedHla donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus;
        private DonorInfoWithExpandedHla donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus;
        private DonorInfoWithExpandedHla donorInfoWithNoMatchAtLocus;
        private DonorInfoWithExpandedHla donorInfoWithFullCrossHeterozygousMatchAtLocus;

        private const DonorType DefaultDonorType = DonorType.Cord;

        private const string MatchingPGroup = "dummy-matching-p-group";
        private readonly Locus locus;
        private readonly List<string> patientPGroupsAtPositionOne = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionOne };
        private readonly List<string> patientPGroupsAtPositionTwo = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo };

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
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorMatchingService>();
        }

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var updateRepo = repositoryFactory.GetDonorUpdateRepository();

            var defaultRequiredHla = new ExpandedHlaBuilder()
                .WithPGroups(MatchingPGroup)
                .Build();

            donorInfoWithFullHomozygousMatchAtLocus = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtBothPositions, PatientPGroupAtPositionOne)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtBothPositions, "non-matching-pgroup")
                        .Build()
                )
                .WithDefaultRequiredHla(defaultRequiredHla)
                .WithDonorType(DefaultDonorType)
                .Build();

            donorInfoWithFullExactHeterozygousMatchAtLocus = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtBothPositions, "non-matching-pgroup")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtPositionTwo, "non-matching-pgroup")
                        .Build()
                )
                .WithDefaultRequiredHla(defaultRequiredHla)
                .WithDonorType(DefaultDonorType)
                .Build();

            donorInfoWithFullCrossHeterozygousMatchAtLocus = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtPositionTwo)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtPositionOne)
                        .Build()
                )
                .WithDefaultRequiredHla(defaultRequiredHla)
                .WithDonorType(DefaultDonorType)
                .Build();

            donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtBothPositions, "non-matching-pgroup")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .WithDefaultRequiredHla(defaultRequiredHla)
                .WithDonorType(DefaultDonorType)
                .Build();

            donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHlaBuilder()
                        .WithPGroups(PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo)
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .WithDefaultRequiredHla(defaultRequiredHla)
                .WithDonorType(DefaultDonorType)
                .Build();

            donorInfoWithNoMatchAtLocus = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build(),
                    new ExpandedHlaBuilder()
                        .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                        .Build()
                )
                .WithDefaultRequiredHla(defaultRequiredHla)
                .WithDonorType(DefaultDonorType)
                .Build();

            var allDonors = new List<DonorInfoWithExpandedHla>
            {
                donorInfoWithFullHomozygousMatchAtLocus,
                donorInfoWithFullExactHeterozygousMatchAtLocus,
                donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus,
                donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus,
                donorInfoWithNoMatchAtLocus,
                donorInfoWithFullCrossHeterozygousMatchAtLocus
            };

            Task.Run(() => updateRepo.InsertBatchOfDonorsWithExpandedHla(allDonors)).Wait();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            DatabaseManager.ClearDatabases();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());
            results.Should().NotContain(d => d.DonorInfo.DonorId == donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.Should().NotContain(d => d.DonorInfo.DonorId == donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
            results.Should().NotContain(d => d.DonorInfo.DonorId == donorInfoWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_MatchesDonorsWithTwoMatchesAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_MatchesDonorWithOneOfTwoHvGAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_MatchesDonorWithOneOfTwoBothDirectionsAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_DoesNotMatchDonorsWithNoMatchAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 1,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().NotContain(d => d.DonorInfo.DonorId == donorInfoWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_MatchesDonorWithNoMatchAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 2,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_MatchesDonorsWithExactMatchAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 2,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_MatchesDonorsWithSingleMatchAtLocus()
        {
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 2,
                    PGroupsToMatchInPositionOne = patientPGroupsAtPositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtPositionTwo,
                })
                .Build();
            var results = await matchingService.GetMatches(criteria);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.Should().Contain(d => d.DonorInfo.DonorId == donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
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
                    PGroupsToMatchInPositionOne = new[] { MatchingPGroup },
                    PGroupsToMatchInPositionTwo = new[] { MatchingPGroup }
                })
                .WithSearchType(DefaultDonorType)
                .WithDonorMismatchCount(0);
        }
    }
}