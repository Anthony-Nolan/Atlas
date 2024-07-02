using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
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
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;
using LochNessBuilder;
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

        private readonly TestHlaMetadata defaultRequiredHla = new TestHlaBuilder().WithPGroups(MatchingPGroup).Build();
        private const DonorType DefaultDonorType = DonorType.Cord;

        private static string NonMatchingPGroup(int index = 0) => $"non-matching-p-group-{index}";

        private DonorInfoWithTestHlaBuilder DefaultDonorBuilder() => new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
            .WithDonorType(DefaultDonorType)
            .WithDefaultRequiredHla(defaultRequiredHla);

        private DonorInfoWithExpandedHla donorInfoWithFullHomozygousMatchAtLocus;

        private DonorInfoWithExpandedHla DonorInfoWithFullHomozygousMatchAtLocus => donorInfoWithFullHomozygousMatchAtLocus ??= DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                locus,
                new[] {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne},
                new[] {PatientPGroupAtBothPositions, "non-matching-p-group"})
            .Build();

        private DonorInfoWithExpandedHla donorInfoWithFullExactHeterozygousMatchAtLocus;

        private DonorInfoWithExpandedHla DonorInfoWithFullExactHeterozygousMatchAtLocus => donorInfoWithFullExactHeterozygousMatchAtLocus ??=
            DefaultDonorBuilder()
                .WithPGroupsAtLocus(
                    locus,
                    new[] {PatientPGroupAtBothPositions, NonMatchingPGroup()},
                    new[] {PatientPGroupAtPositionTwo, NonMatchingPGroup()})
                .Build();

        private DonorInfoWithExpandedHla donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus;

        private DonorInfoWithExpandedHla DonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus =>
            donorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus ??= DefaultDonorBuilder()
                .WithPGroupsAtLocus(
                    locus,
                    new[] {PatientPGroupAtBothPositions, NonMatchingPGroup(1)},
                    new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)})
                .Build();

        private DonorInfoWithExpandedHla donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus;

        private DonorInfoWithExpandedHla DonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus =>
            donorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus ??= DefaultDonorBuilder()
                .WithPGroupsAtLocus(
                    locus,
                    new[] {PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo},
                    new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)})
                .Build();

        private DonorInfoWithExpandedHla donorInfoWithNoMatchAtLocus;

        private DonorInfoWithExpandedHla DonorInfoWithNoMatchAtLocus => donorInfoWithNoMatchAtLocus ??= DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                locus,
                new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)},
                new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)})
            .Build();

        private DonorInfoWithExpandedHla donorInfoWithFullCrossHeterozygousMatchAtLocus;

        private DonorInfoWithExpandedHla DonorInfoWithFullCrossHeterozygousMatchAtLocus => donorInfoWithFullCrossHeterozygousMatchAtLocus ??=
            DefaultDonorBuilder()
                .WithPGroupsAtLocus(locus, PatientPGroupAtPositionTwo, PatientPGroupAtPositionOne)
                .Build();


        private const string MatchingPGroup = "dummy-matching-p-group";
        private readonly Locus locus;
        private readonly List<string> patientPGroupsAtPositionOne = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne};
        private readonly List<string> patientPGroupsAtPositionTwo = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo};

        private const string PatientPGroupAtBothPositions = "patient-p-group-at-both-positions";
        private const string PatientPGroupAtPositionOne = "patient-p-group-at-position-one";
        private const string PatientPGroupAtPositionTwo = "patient-p-group-at-position-two";

        public MatchingTestsAtASingleLocus(Locus locus)
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

                var allDonors = new List<DonorInfoWithExpandedHla>
                {
                    DonorInfoWithFullHomozygousMatchAtLocus,
                    DonorInfoWithFullExactHeterozygousMatchAtLocus,
                    DonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus,
                    DonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus,
                    DonorInfoWithNoMatchAtLocus,
                    DonorInfoWithFullCrossHeterozygousMatchAtLocus
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
            var results = await matchingService.GetMatches(new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()).Build(), null).ToListAsync();
            results.ShouldContainDonor(DonorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()).Build(), null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(DonorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocus()
        {
            var results = await matchingService.GetMatches(new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()).Build(), null).ToListAsync();
            results.ShouldNotContainDonor(DonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.ShouldNotContainDonor(DonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
            results.ShouldNotContainDonor(DonorInfoWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_MatchesDonorsWithTwoMatchesAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(1)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(DonorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(DonorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_MatchesDonorWithOneOfTwoHvGAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(1)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_MatchesDonorWithOneOfTwoBothDirectionsAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(1)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_DoesNotMatchDonorsWithNoMatchAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(1)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldNotContainDonor(DonorInfoWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_MatchesDonorWithNoMatchAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(2)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithNoMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_MatchesDonorsWithExactMatchAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(2)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithFullExactHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(DonorInfoWithFullCrossHeterozygousMatchAtLocus.DonorId);
            results.ShouldContainDonor(DonorInfoWithFullHomozygousMatchAtLocus.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_MatchesDonorsWithSingleMatchAtLocus()
        {
            var criteria = new MatchCriteriaBuilder(GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().WithMismatchCount(2)))
                .Build();

            var results = await matchingService.GetMatches(criteria, null).ToListAsync();

            results.ShouldContainDonor(DonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocus.DonorId);
            results.ShouldContainDonor(DonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocus.DonorId);
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder() =>
            new AlleleLevelMatchCriteriaBuilder()
                .WithShouldIncludeBetterMatches(true)
                .WithLocusMatchCriteria(locus, DefaultPatientLocusCriteriaBuilder().Build())
                .WithDefaultLocusMatchCriteria(DefaultMatchingCriteriaBuilder().Build())
                .WithSearchType(DefaultDonorType)
                .WithDonorMismatchCount(0);

        private Builder<AlleleLevelLocusMatchCriteria> DefaultMatchingCriteriaBuilder() =>
            AlleleLevelLocusMatchCriteriaBuilder.New
                .WithMismatchCount(0)
                .WithPGroups(new[] {MatchingPGroup}, new[] {MatchingPGroup});

        private Builder<AlleleLevelLocusMatchCriteria> DefaultPatientLocusCriteriaBuilder() =>
            AlleleLevelLocusMatchCriteriaBuilder.New
                .WithMismatchCount(0)
                .WithPGroups(patientPGroupsAtPositionOne, patientPGroupsAtPositionTwo);
    }
}