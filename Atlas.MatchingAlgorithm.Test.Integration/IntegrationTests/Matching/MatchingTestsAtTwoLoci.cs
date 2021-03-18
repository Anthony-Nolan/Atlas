using System.Collections.Generic;
using System.Linq;
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

// ReSharper disable InconsistentNaming

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Matching
{
    [TestFixture(Locus.A, Locus.B)]
    [TestFixture(Locus.A, Locus.C)]
    [TestFixture(Locus.A, Locus.Drb1)]
    [TestFixture(Locus.A, Locus.Dqb1)]
    [TestFixture(Locus.B, Locus.C)]
    [TestFixture(Locus.B, Locus.Drb1)]
    [TestFixture(Locus.B, Locus.Dqb1)]
    [TestFixture(Locus.Drb1, Locus.C)]
    [TestFixture(Locus.Drb1, Locus.Dqb1)]
    [TestFixture(Locus.C, Locus.Dqb1)]
    public class MatchingTestsAtTwoLoci
    {
        private IMatchingService matchingService;

        private DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtEitherLocus;
        private DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtLocus1AndHalfMatchAtLocus2;
        private DonorInfoWithExpandedHla cordDonorInfoWithNoMatchAtLocus1AndFullMatchAtLocus2;

        private DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchAtLocus1AndNoMatchAtLocus2;
        private DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchAtBothLoci;
        private DonorInfoWithExpandedHla cordDonorInfoWithHalfMatchAtLocus1AndFullMatchAtLocus2;

        private DonorInfoWithExpandedHla cordDonorInfoWithFullMatchAtLocus1AndNoMatchAtLocus2;
        private DonorInfoWithExpandedHla cordDonorInfoWithFullMatchAtLocus1AndHalfMatchAtLocus2;
        private DonorInfoWithExpandedHla cordDonorInfoWithFullMatchAtBothLoci;

        private const DonorType DefaultDonorType = DonorType.Cord;

        private static readonly List<string> matchingPGroups = new List<string> {"dummy-matching-p-group"};
        private readonly Locus locus1;
        private readonly Locus locus2;

        private const string PatientPGroupAtLocusOne_PositionOne = "patient-p-group-at-locus-one-position-1";
        private const string PatientPGroupAtLocusTwo_PositionOne = "patient-p-group-at-locus-two-position-1";

        private const string PatientPGroupAtLocusOne_PositionTwo = "patient-p-group-at-locus-one-position-2";
        private const string PatientPGroupAtLocusTwo_PositionTwo = "patient-p-group-at-locus-two-position-2";

        private const string NonMatchingPGroup = "non-matching-p-group";

        private readonly List<string> patientPGroupsAtLocusOne_PositionOne = new List<string> {PatientPGroupAtLocusOne_PositionOne};
        private readonly List<string> patientPGroupsAtLocusOne_PositionTwo = new List<string> {PatientPGroupAtLocusOne_PositionTwo};

        private readonly List<string> patientPGroupsAtLocusTwo_PositionOne = new List<string> {PatientPGroupAtLocusTwo_PositionOne};
        private readonly List<string> patientPGroupsAtLocusTwo_PositionTwo = new List<string> {PatientPGroupAtLocusTwo_PositionTwo};


        public MatchingTestsAtTwoLoci(Locus locus1, Locus locus2)
        {
            this.locus1 = locus1;
            this.locus2 = locus2;
        }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchingService>();
        }

        /// <summary>
        /// Set up test data. This test suite is only testing variations on Locus A - the other required loci should be assumed to always match
        /// </summary>
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var donorRepo = repositoryFactory.GetDonorUpdateRepository();

                cordDonorInfoWithNoMatchAtEitherLocus = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(0)
                        .WithMatchesAtLocus2(0)
                        .Build();

                cordDonorInfoWithNoMatchAtLocus1AndHalfMatchAtLocus2 = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(0)
                        .WithMatchesAtLocus2(1)
                        .Build();

                cordDonorInfoWithNoMatchAtLocus1AndFullMatchAtLocus2 = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(0)
                        .WithMatchesAtLocus2(2)
                        .Build();

                cordDonorInfoWithHalfMatchAtLocus1AndNoMatchAtLocus2 = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(1)
                        .WithMatchesAtLocus2(0)
                        .Build();

                cordDonorInfoWithHalfMatchAtBothLoci = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(1)
                        .WithMatchesAtLocus2(1)
                        .Build();

                cordDonorInfoWithHalfMatchAtLocus1AndFullMatchAtLocus2 = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(1)
                        .WithMatchesAtLocus2(2)
                        .Build();

                cordDonorInfoWithFullMatchAtLocus1AndNoMatchAtLocus2 = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(2)
                        .WithMatchesAtLocus2(0)
                        .Build();

                cordDonorInfoWithFullMatchAtLocus1AndHalfMatchAtLocus2 = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(2)
                        .WithMatchesAtLocus2(1)
                        .Build();

                cordDonorInfoWithFullMatchAtBothLoci = new TwoLociTestsDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                        .WithMatchesAtLocus1(2)
                        .WithMatchesAtLocus2(2)
                        .Build();

                var allDonors = new List<DonorInfoWithExpandedHla>
                {
                    cordDonorInfoWithNoMatchAtEitherLocus,
                    cordDonorInfoWithNoMatchAtLocus1AndHalfMatchAtLocus2,
                    cordDonorInfoWithNoMatchAtLocus1AndFullMatchAtLocus2,
                    cordDonorInfoWithHalfMatchAtBothLoci,
                    cordDonorInfoWithHalfMatchAtLocus1AndNoMatchAtLocus2,
                    cordDonorInfoWithHalfMatchAtLocus1AndFullMatchAtLocus2,
                    cordDonorInfoWithFullMatchAtBothLoci,
                    cordDonorInfoWithFullMatchAtLocus1AndHalfMatchAtLocus2,
                    cordDonorInfoWithFullMatchAtLocus1AndNoMatchAtLocus2
                };

                Task.Run(() => donorRepo.InsertBatchOfDonorsWithExpandedHla(allDonors, false)).Wait();
            });
        }
        
        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }
        
        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus1_DoesNotMatchDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchCount(locus1, 2)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldNotContainDonor(cordDonorInfoWithNoMatchAtLocus1AndHalfMatchAtLocus2.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatches_TwoAtLocus1_OneAtLocus2_MatchesDonorsWithExactMatchAtBothLoci()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldContainDonor(cordDonorInfoWithFullMatchAtBothLoci.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatches_TwoAtLocus1_OneAtLocus2_MatchesDonorsWithSingleMatchAtLocus1AndFullMatchAtLocus2()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldContainDonor(cordDonorInfoWithHalfMatchAtLocus1AndFullMatchAtLocus2.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatches_TwoAtLocus1_OneAtLocus2_MatchesDonorsWithNoMatchAtLocus1()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldContainDonor(cordDonorInfoWithNoMatchAtLocus1AndFullMatchAtLocus2.DonorId);
            results.ShouldContainDonor(cordDonorInfoWithNoMatchAtLocus1AndHalfMatchAtLocus2.DonorId);
        }
        
        [Test]
        public async Task Search_WithFourAllowedMismatches_TwoAtLocus1_TwoAtLocus2_MatchesDonorsWithNoMatchAtEitherLocus()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(4)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 2)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria, null).ToListAsync();
            results.ShouldContainDonor(cordDonorInfoWithNoMatchAtEitherLocus.DonorId);
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
                .WithShouldIncludeBetterMatches(true)
                .WithLocusMatchCriteria(locus1, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = patientPGroupsAtLocusOne_PositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtLocusOne_PositionTwo
                })
                .WithLocusMatchCriteria(locus2, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = patientPGroupsAtLocusTwo_PositionOne,
                    PGroupsToMatchInPositionTwo = patientPGroupsAtLocusTwo_PositionTwo
                })
                .WithDefaultLocusMatchCriteria(new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = matchingPGroups,
                    PGroupsToMatchInPositionTwo = matchingPGroups
                })
                .WithSearchType(DefaultDonorType)
                .WithDonorMismatchCount(0);
        }

        private class TwoLociTestsDonorBuilder
        {
            private readonly Locus locus1;
            private readonly Locus locus2;
            private DonorInfoWithTestHlaBuilder DonorResultBuilder;

            public TwoLociTestsDonorBuilder(int donorId, Locus locus1, Locus locus2)
            {
                this.locus1 = locus1;
                this.locus2 = locus2;
                DonorResultBuilder = new DonorInfoWithTestHlaBuilder(donorId)
                    .WithDonorType(DonorType.Cord);
            }

            public TwoLociTestsDonorBuilder WithMatchesAtLocus1(int numberOfMatches)
            {
                var pGroupAtPosition1 = numberOfMatches > 0 ? PatientPGroupAtLocusOne_PositionOne : NonMatchingPGroup;
                var pGroupAtPosition2 = numberOfMatches > 1 ? PatientPGroupAtLocusOne_PositionTwo : NonMatchingPGroup;

                DonorResultBuilder = DonorResultBuilder
                    .WithHlaAtLocus(
                        locus1,
                        new TestHlaBuilder().WithPGroups(pGroupAtPosition1).Build(),
                        new TestHlaBuilder().WithPGroups(pGroupAtPosition2).Build());

                return this;
            }

            public TwoLociTestsDonorBuilder WithMatchesAtLocus2(int numberOfMatches)
            {
                var pGroupAtPosition1 = numberOfMatches > 0 ? PatientPGroupAtLocusTwo_PositionOne : NonMatchingPGroup;
                var pGroupAtPosition2 = numberOfMatches > 1 ? PatientPGroupAtLocusTwo_PositionTwo : NonMatchingPGroup;

                DonorResultBuilder = DonorResultBuilder
                    .WithHlaAtLocus(
                        locus2,
                        new TestHlaBuilder().WithPGroups(pGroupAtPosition1).Build(),
                        new TestHlaBuilder().WithPGroups(pGroupAtPosition2).Build());

                return this;
            }

            public DonorInfoWithExpandedHla Build()
            {
                return DonorResultBuilder
                    .WithDefaultRequiredHla(new TestHlaMetadata
                    {
                        LookupName = "hla-name",
                        MatchingPGroups = matchingPGroups
                    })
                    .Build();
            }
        }
    }
}