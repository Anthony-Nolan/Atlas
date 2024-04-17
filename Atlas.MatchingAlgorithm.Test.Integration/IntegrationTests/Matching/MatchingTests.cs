using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.TestHelpers;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public class MatchingTests
    {
        private IMatchingService matchingService;

        private const DonorType DefaultDonorType = DonorType.Cord;

        /// <returns> An input donor builder pre-populated with default donor data of an exact match. </returns>
        private static DonorInfoWithTestHlaBuilder DefaultDonorBuilder() =>
            new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithDonorType(DefaultDonorType)
                .WithPGroupsAtLocus(Locus.A, PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo)
                .WithPGroupsAtLocus(Locus.B, PatientPGroup_LocusB_PositionOne, PatientPGroup_LocusB_PositionTwo)
                .WithPGroupsAtLocus(Locus.Drb1, PatientPGroup_LocusDRB1_PositionOne, PatientPGroup_LocusDRB1_PositionTwo);

        private static string NonMatchingPGroup(int index = 0) => $"non-matching-p-group-{index}";

        private const string PatientPGroup_LocusA_BothPositions = "01:01P";

        private const string PatientPGroup_LocusA_PositionOne = "01:02";

        private const string PatientPGroup_LocusA_PositionTwo = "02:01";

        private const string PatientPGroup_LocusB_PositionOne = "07:02P";

        private const string PatientPGroup_LocusB_PositionTwo = "08:01P";

        private const string PatientPGroup_LocusDRB1_PositionOne = "01:11P";

        private const string PatientPGroup_LocusDRB1_PositionTwo = "03:41P";

        private readonly DonorInfoWithExpandedHla cordDonorInfoWithFullHomozygousMatchAtLocusA = DefaultDonorBuilder().Build();

        private readonly DonorInfoWithExpandedHla cordDonorInfoWithFullHeterozygousMatchAtLocusA = DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                Locus.A,
                new[] {PatientPGroup_LocusA_BothPositions, NonMatchingPGroup()},
                new[] {PatientPGroup_LocusA_PositionTwo, NonMatchingPGroup()})
            .Build();

        private readonly DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                Locus.A,
                new[] {PatientPGroup_LocusA_BothPositions, NonMatchingPGroup()},
                new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)})
            .Build();

        private readonly DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                Locus.A,
                new[] {PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo},
                new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)})
            .Build();

        private readonly DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtLocusAAndExactMatchAtB = DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                Locus.A,
                new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)},
                new[] {NonMatchingPGroup(3), NonMatchingPGroup(4)})
            .WithPGroupsAtLocus(Locus.B, PatientPGroup_LocusB_PositionOne, PatientPGroup_LocusB_PositionTwo)
            .Build();

        private readonly DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtLocusAAndHalfMatchAtB = DefaultDonorBuilder()
            .WithPGroupsAtLocus(
                Locus.A,
                new[] {NonMatchingPGroup(1), NonMatchingPGroup(2)},
                new[] {NonMatchingPGroup(3), NonMatchingPGroup(4)})
            .WithPGroupsAtLocus(Locus.B, PatientPGroup_LocusB_PositionOne, NonMatchingPGroup())
            .Build();

        private readonly DonorInfoWithExpandedHla adultDonorInfoWithFullMatch = DefaultDonorBuilder()
            .WithDonorType(DonorType.Adult)
            .Build();

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchingService>();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();

                AddTestDonorsAvailableForSearch(donorUpdateRepository);
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }

        private void AddTestDonorsAvailableForSearch(IDonorUpdateRepository donorUpdateRepository)
        {
            var allDonors = new List<DonorInfoWithExpandedHla>
            {
                cordDonorInfoWithFullHomozygousMatchAtLocusA,
                cordDonorInfoWithFullHeterozygousMatchAtLocusA,
                cordDonorInfoWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA,
                cordDonorInfoWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA,
                cordDonorInfoWithNoMatchAtLocusAAndExactMatchAtB,
                cordDonorInfoWithNoMatchAtLocusAAndHalfMatchAtB,
                adultDonorInfoWithFullMatch
            };

            Task.Run(() => donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(allDonors, false)).Wait();
        }

        private static async Task AddTestDonorUnavailableForSearch(DonorInfoWithExpandedHla donorInfoWithExpandedHla)
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorUpdateRepository = repositoryFactory.GetDonorUpdateRepository();

            await donorUpdateRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donorInfoWithExpandedHla}, false);
            await donorUpdateRepository.SetDonorBatchAsUnavailableForSearch(new[] {donorInfoWithExpandedHla.DonorId});
        }

        [Test]
        public async Task GetMatches_ForAdultDonors_DoesNotMatchCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.Should().NotContain(d => d.DonorInfo.DonorId == cordDonorInfoWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().NotContain(d => d.DonorInfo.DonorId == cordDonorInfoWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_DoesNotMatchAdultDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.Should().NotContain(d => d.DonorInfo.DonorId == adultDonorInfoWithFullMatch.DonorId);
        }

        [Test]
        public async Task GetMatches_ForCordDonors_MatchesCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldContainDonor(cordDonorInfoWithFullHeterozygousMatchAtLocusA.DonorId);
            results.ShouldContainDonor(cordDonorInfoWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_WhenBetterMatchesDisallowed_DoesNotMatchDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithShouldIncludeBetterMatches(false)
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.Should().NotContain(d => d.DonorInfo.DonorId == adultDonorInfoWithFullMatch.DonorId);
        }

        [Test]
        public async Task GetMatches_WhenBetterMatchesAllowed_MatchesDonorsWithFewerMismatchesThanSpecified()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldContainDonor(cordDonorInfoWithFullHeterozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task GetMatches_DoesNotReturnMatchingDonorThatIsUnavailableForSearch()
        {
            var unavailableDonor = DefaultDonorBuilder().Build();
            await AddTestDonorUnavailableForSearch(unavailableDonor);

            var searchCriteria = GetDefaultCriteriaBuilder().Build();

            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldNotContainDonor(unavailableDonor.DonorId);
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private static MatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new MatchCriteriaBuilder()
                .WithSearchType(DefaultDonorType)
                .WithShouldIncludeBetterMatches(true)
                .WithDonorMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new[] {PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionOne},
                    PGroupsToMatchInPositionTwo = new[] {PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionTwo}
                })
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new[] {PatientPGroup_LocusB_PositionOne},
                    PGroupsToMatchInPositionTwo = new[] {PatientPGroup_LocusB_PositionTwo}
                })
                .WithLocusMatchCriteria(Locus.Drb1, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new[] {PatientPGroup_LocusDRB1_PositionOne},
                    PGroupsToMatchInPositionTwo = new[] {PatientPGroup_LocusDRB1_PositionTwo}
                })
                .WithDonorMismatchCount(0);
        }
    }

    internal static class BuilderExtensions
    {
        public static DonorInfoWithTestHlaBuilder WithPGroupsAtLocus(
            this DonorInfoWithTestHlaBuilder builder,
            Locus locus,
            IEnumerable<string> pGroups1,
            IEnumerable<string> pGroups2) =>
            builder.WithHlaAtLocus(
                locus,
                new TestHlaBuilder().WithPGroups(pGroups1.ToArray()).Build(),
                new TestHlaBuilder().WithPGroups(pGroups2.ToArray()).Build()
            );

        public static DonorInfoWithTestHlaBuilder WithPGroupsAtLocus(
            this DonorInfoWithTestHlaBuilder builder,
            Locus locus,
            string pGroup1,
            IEnumerable<string> pGroups2) =>
            builder.WithPGroupsAtLocus(locus, new[] {pGroup1}, pGroups2);

        public static DonorInfoWithTestHlaBuilder WithPGroupsAtLocus(
            this DonorInfoWithTestHlaBuilder builder,
            Locus locus,
            string pGroup1,
            string pGroup2) =>
            builder.WithPGroupsAtLocus(locus, new[] {pGroup1}, new[] {pGroup2});

        public static DonorInfoWithTestHlaBuilder WithPGroupsAtLocus(
            this DonorInfoWithTestHlaBuilder builder,
            Locus locus,
            IEnumerable<string> pGroups1,
            string pGroup2) =>
            builder.WithPGroupsAtLocus(locus, pGroups1, new[] {pGroup2});
    }
}