using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Matching
{
    [TestFixture(Locus.A)]
    [TestFixture(Locus.B)]
    [TestFixture(Locus.Drb1)]
    [TestFixture(Locus.Dqb1)]
    [TestFixture(Locus.C)]
    public class MatchingTestsAtASingleLocus
    {
        private IMatchingService matchingService;
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
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchingService>();
        }

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var updateRepo = repositoryFactory.GetDonorUpdateRepository();

                var defaultRequiredHla = new TestHlaBuilder()
                    .WithPGroups(MatchingPGroup)
                    .Build();

                donorInfoWithFullHomozygousMatchAtLocus = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                    .WithHlaAtLocus(
                        locus,
                        new TestHlaBuilder()
                            .WithPGroups(PatientPGroupAtBothPositions, PatientPGroupAtPositionOne)
                            .Build(),
                        new TestHlaBuilder()
                            .WithPGroups(PatientPGroupAtBothPositions, "non-matching-pgroup")
                            .Build()
                    )
                    .WithDefaultRequiredHla(defaultRequiredHla)
                    .WithDonorType(DefaultDonorType)
                    .Build();

                donorInfoWithFullExactHeterozygousMatchAtLocus = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                        .WithHlaAtLocus(
                            locus,
                            new TestHlaBuilder()
                                .WithPGroups(PatientPGroupAtBothPositions, "non-matching-pgroup")
                                .Build(),
                            new TestHlaBuilder()
                                .WithPGroups(PatientPGroupAtPositionTwo, "non-matching-pgroup")
                                .Build()
                        )
                        .WithDefaultRequiredHla(defaultRequiredHla)
                        .WithDonorType(DefaultDonorType)
                        .Build();

                donorInfoWithFullCrossHeterozygousMatchAtLocus = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                        .WithHlaAtLocus(
                            locus,
                            new TestHlaBuilder()
                                .WithPGroups(PatientPGroupAtPositionTwo)
                                .Build(),
                            new TestHlaBuilder()
                                .WithPGroups(PatientPGroupAtPositionOne)
                                .Build()
                        )
                        .WithDefaultRequiredHla(defaultRequiredHla)
                        .WithDonorType(DefaultDonorType)
                        .Build();

                donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                        .WithHlaAtLocus(
                            locus,
                            new TestHlaBuilder()
                                .WithPGroups(PatientPGroupAtBothPositions, "non-matching-pgroup")
                                .Build(),
                            new TestHlaBuilder()
                                .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                                .Build()
                        )
                        .WithDefaultRequiredHla(defaultRequiredHla)
                        .WithDonorType(DefaultDonorType)
                        .Build();

                donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                        .WithHlaAtLocus(
                            locus,
                            new TestHlaBuilder()
                                .WithPGroups(PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo)
                                .Build(),
                            new TestHlaBuilder()
                                .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                                .Build()
                        )
                        .WithDefaultRequiredHla(defaultRequiredHla)
                        .WithDonorType(DefaultDonorType)
                        .Build();

                donorInfoWithNoMatchAtLocus = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                    .WithHlaAtLocus(
                        locus,
                        new TestHlaBuilder()
                            .WithPGroups("non-matching-pgroup", "non-matching-pgroup-2")
                            .Build(),
                        new TestHlaBuilder()
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

                Task.Run(() => updateRepo.InsertBatchOfDonorsWithExpandedHla(allDonors, false)).Wait();
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());
            results.ShouldContainDonor(donorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());
            results.ShouldContainDonor(donorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(donorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());
            results.ShouldNotContainDonor(donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.ShouldNotContainDonor(donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
            results.ShouldNotContainDonor(donorInfoWithNoMatchAtLocus.DonorId);
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
            results.ShouldContainDonor(donorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(donorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(donorInfoWithFullHomozygousMatchAtLocus.DonorId);
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
            results.ShouldContainDonor(donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
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
            results.ShouldContainDonor(donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
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
            results.ShouldNotContainDonor(donorInfoWithNoMatchAtLocus.DonorId);
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
            results.ShouldContainDonor(donorInfoWithNoMatchAtLocus.DonorId);
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
            results.ShouldContainDonor(donorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(donorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(donorInfoWithFullHomozygousMatchAtLocus.DonorId);
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
            results.ShouldContainDonor(donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.ShouldContainDonor(donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
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