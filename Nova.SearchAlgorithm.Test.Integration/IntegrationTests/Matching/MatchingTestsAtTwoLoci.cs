using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
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
    public class MatchingTestsAtTwoLoci : IntegrationTestBase
    {
        private IDonorMatchingService matchingService;

        private InputDonorWithExpandedHla cordDonorWithNoMatchAtEitherLocus;
        private InputDonorWithExpandedHla cordDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2;
        private InputDonorWithExpandedHla cordDonorWithNoMatchAtLocus1AndFullMatchAtLocus2;

        private InputDonorWithExpandedHla cordDonorWithHalfMatchAtLocus1AndNoMatchAtLocus2;
        private InputDonorWithExpandedHla cordDonorWithHalfMatchAtBothLoci;
        private InputDonorWithExpandedHla cordDonorWithHalfMatchAtLocus1AndFullMatchAtLocus2;

        private InputDonorWithExpandedHla cordDonorWithFullMatchAtLocus1AndNoMatchAtLocus2;
        private InputDonorWithExpandedHla cordDonorWithFullMatchAtLocus1AndHalfMatchAtLocus2;
        private InputDonorWithExpandedHla cordDonorWithFullMatchAtBothLoci;

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


        public MatchingTestsAtTwoLoci(Locus locus1, Locus locus2) : base()
        {
            this.locus1 = locus1;
            this.locus2 = locus2;
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

            cordDonorWithNoMatchAtEitherLocus = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(0)
                .WithMatchesAtLocus2(0)
                .Build();
            
            cordDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2 = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(0)
                .WithMatchesAtLocus2(1)
                .Build();
            
            cordDonorWithNoMatchAtLocus1AndFullMatchAtLocus2 = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(0)
                .WithMatchesAtLocus2(2)
                .Build();
            
            cordDonorWithHalfMatchAtLocus1AndNoMatchAtLocus2 = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(1)
                .WithMatchesAtLocus2(0)
                .Build();
            
            cordDonorWithHalfMatchAtBothLoci = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(1)
                .WithMatchesAtLocus2(1)
                .Build();
            
            cordDonorWithHalfMatchAtLocus1AndFullMatchAtLocus2 = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(1)
                .WithMatchesAtLocus2(2)
                .Build();
            
            cordDonorWithFullMatchAtLocus1AndNoMatchAtLocus2 = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(2)
                .WithMatchesAtLocus2(0)
                .Build();
            
            cordDonorWithFullMatchAtLocus1AndHalfMatchAtLocus2= new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(2)
                .WithMatchesAtLocus2(1)
                .Build();
            
            cordDonorWithFullMatchAtBothLoci = new TwoLociTestsInputDonorBuilder(DonorIdGenerator.NextId(), locus1, locus2)
                .WithMatchesAtLocus1(2)
                .WithMatchesAtLocus2(2)
                .Build();

            var allDonors = new List<InputDonorWithExpandedHla>
            {
                cordDonorWithNoMatchAtEitherLocus,
                cordDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2,
                cordDonorWithNoMatchAtLocus1AndFullMatchAtLocus2,
                cordDonorWithHalfMatchAtBothLoci,
                cordDonorWithHalfMatchAtLocus1AndNoMatchAtLocus2,
                cordDonorWithHalfMatchAtLocus1AndFullMatchAtLocus2, 
                cordDonorWithFullMatchAtBothLoci, 
                cordDonorWithFullMatchAtLocus1AndHalfMatchAtLocus2,
                cordDonorWithFullMatchAtLocus1AndNoMatchAtLocus2
            };
            foreach (var donor in allDonors)
            {
                Task.Run(() => importRepo.AddDonorWithHla(donor)).Wait();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus1_DoesNotMatchDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchCount(locus1, 2)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatches_TwoAtLocus1_OneAtLocus2_MatchesDonorsWithExactMatchAtBothLoci()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatchAtBothLoci.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatches_TwoAtLocus1_OneAtLocus2_MatchesDonorsWithSingleMatchAtLocus1AndFullMatchAtLocus2()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 1)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithHalfMatchAtLocus1AndFullMatchAtLocus2.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatches_TwoAtLocus1_OneAtLocus2_MatchesDonorsWithNoMatchAtLocus1()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 1)
                .Build();
            var results = (await matchingService.GetMatches(searchCriteria)).ToList();
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithNoMatchAtLocus1AndFullMatchAtLocus2.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithNoMatchAtLocus1AndHalfMatchAtLocus2.DonorId);
        }
        
        [Test]
        public async Task Search_WithFourAllowedMismatches_TwoAtLocus1_TwoAtLocus2_MatchesDonorsWithNoMatchAtEitherLocus()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(4)
                .WithLocusMismatchCount(locus1, 2)
                .WithLocusMismatchCount(locus2, 2)
                .Build();
            var results = await matchingService.GetMatches(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithNoMatchAtEitherLocus.DonorId);
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
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

        private class TwoLociTestsInputDonorBuilder
        {
            private readonly Locus locus1;
            private readonly Locus locus2;
            private InputDonorWithExpandedHlaBuilder inputDonorWithExpandedHlaBuilder;

            public TwoLociTestsInputDonorBuilder(int donorId, Locus locus1, Locus locus2)
            {
                this.locus1 = locus1;
                this.locus2 = locus2;
                inputDonorWithExpandedHlaBuilder = new InputDonorWithExpandedHlaBuilder(donorId)
                    .WithDonorType(DonorType.Cord);
            }

            public TwoLociTestsInputDonorBuilder WithMatchesAtLocus1(int numberOfMatches)
            {
                var pGroupAtPosition1 = numberOfMatches > 0 ? PatientPGroupAtLocusOne_PositionOne : NonMatchingPGroup;
                var pGroupAtPosition2 = numberOfMatches > 1 ? PatientPGroupAtLocusOne_PositionTwo : NonMatchingPGroup;

                inputDonorWithExpandedHlaBuilder = inputDonorWithExpandedHlaBuilder
                    .WithMatchingHlaAtLocus(
                        locus1,
                        new ExpandedHlaBuilder().WithPGroups(pGroupAtPosition1).Build(),
                        new ExpandedHlaBuilder().WithPGroups(pGroupAtPosition2).Build());

                return this;
            }

            public TwoLociTestsInputDonorBuilder WithMatchesAtLocus2(int numberOfMatches)
            {
                var pGroupAtPosition1 = numberOfMatches > 0 ? PatientPGroupAtLocusTwo_PositionOne : NonMatchingPGroup;
                var pGroupAtPosition2 = numberOfMatches > 1 ? PatientPGroupAtLocusTwo_PositionTwo : NonMatchingPGroup;

                inputDonorWithExpandedHlaBuilder = inputDonorWithExpandedHlaBuilder
                    .WithMatchingHlaAtLocus(
                        locus2,
                        new ExpandedHlaBuilder().WithPGroups(pGroupAtPosition1).Build(),
                        new ExpandedHlaBuilder().WithPGroups(pGroupAtPosition2).Build());

                return this;
            }

            public InputDonorWithExpandedHla Build()
            {
                return inputDonorWithExpandedHlaBuilder
                    .WithDefaultRequiredHla(new ExpandedHla {PGroups = matchingPGroups})
                    .Build();
            }
        }
    }
}