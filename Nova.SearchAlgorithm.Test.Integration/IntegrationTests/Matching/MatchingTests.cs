using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public class MatchingTests : IntegrationTestBase
    {
        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocusA;
        private InputDonor donorWithFullHeterozygousMatchAtLocusA;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA;
        private InputDonor donorWithNoMatchAtLocusAAndExactMatchAtB;
        private InputDonor donorWithNoMatchAtLocusAAndHalfMatchAtB;

        // Registries chosen to be different from `DefaultRegistryCode`
        private InputDonor donorWithFullMatchAtAnthonyNolanRegistry;
        private InputDonor donorWithFullMatchAtNmdpRegistry;
        
        private InputDonor cordDonorWithFullMatch;

        private const string PatientPGroup_LocusA_BothPositions = "01:01P";
        private const string PatientPGroup_LocusA_PositionOne = "01:02";
        private const string PatientPGroup_LocusA_PositionTwo = "02:01";
        private const string PatientPGroup_LocusB_PositionOne = "07:02P";
        private const string PatientPGroup_LocusB_PositionTwo = "08:01P";
        private const string PatientPGroup_LocusDRB1_PositionOne = "01:11P";
        private const string PatientPGroup_LocusDRB1_PositionTwo = "03:41P";
        
        private const RegistryCode DefaultRegistryCode = RegistryCode.DKMS;
        private const DonorType DefaultDonorType = DonorType.Adult;

        public MatchingTests(DonorStorageImplementation param) : base(param) { }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = container.Resolve<IDonorMatchingService>();
        }
        
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = container.Resolve<IDonorImportRepository>();

            donorWithFullHomozygousMatchAtLocusA = GetDefaultInputDonorBuilder().Build();

            donorWithFullHeterozygousMatchAtLocusA = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_BothPositions, "non-matching-pgroup"}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionTwo, "non-matching-pgroup"}}
                )
                .Build();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { PatientPGroup_LocusA_BothPositions, "non-matching-pgroup" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } }
                )
                .Build();
                

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { PatientPGroup_LocusA_PositionOne, PatientPGroup_LocusA_PositionTwo } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } }
                )
                .Build();

            donorWithNoMatchAtLocusAAndExactMatchAtB = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup-3", "non-matching-pgroup-4" } }
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionTwo}}
                )
                .Build();

            donorWithNoMatchAtLocusAAndHalfMatchAtB = GetDefaultInputDonorBuilder()
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup", "non-matching-pgroup-2" } },
                    new ExpandedHla { PGroups = new List<string> { "non-matching-pgroup-3", "non-matching-pgroup-4" } }
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionOne}}
                )
                .Build();

            donorWithFullMatchAtAnthonyNolanRegistry = GetDefaultInputDonorBuilder()
                .WithRegistryCode(RegistryCode.AN)
                .Build();

            donorWithFullMatchAtNmdpRegistry = GetDefaultInputDonorBuilder()
                .WithRegistryCode(RegistryCode.NMDP)
                .Build();

            cordDonorWithFullMatch = GetDefaultInputDonorBuilder()
                .WithDonorType(DonorType.Cord)
                .Build();
            
            var allDonors = new List<InputDonor>
            {
                donorWithFullHomozygousMatchAtLocusA,
                donorWithFullHeterozygousMatchAtLocusA,
                donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA,
                donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA,
                donorWithNoMatchAtLocusAAndExactMatchAtB,
                donorWithNoMatchAtLocusAAndHalfMatchAtB,
                donorWithFullMatchAtAnthonyNolanRegistry,
                donorWithFullMatchAtNmdpRegistry,
                cordDonorWithFullMatch
            };

            foreach (var donor in allDonors)
            {
                Task.Run(() => importRepo.AddOrUpdateDonorWithHla(donor)).Wait();
            }
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_DoesNotMatchDonorWithNoMatchAtLocusAAndHalfMatchAtLocusB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMismatchCount(Locus.A, 2)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusAAndHalfMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorsWithExactMatchAtAAndB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorsWithSingleMatchAtAAndCompleteMatchAtB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_ReturnsDonorsWithNoMatchAtLocusA()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusAAndExactMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_WithThreeAllowedMismatchesTwoAtAAndOneAtB_MatchesDonorWithNoMatchAtLocusAAndHalfMatchAtLocusB()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(3)
                .WithLocusMismatchCount(Locus.A, 2)
                .WithLocusMismatchCount(Locus.B, 1)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusAAndHalfMatchAtB.DonorId);
        }

        [Test]
        public async Task Search_ForMultipleSpecifiedRegistries_MatchesDonorsAtAllSpecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> {RegistryCode.AN, RegistryCode.NMDP})
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullMatchAtAnthonyNolanRegistry.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullMatchAtNmdpRegistry.DonorId);
        }
        
        [Test]
        public async Task Search_DoesNotMatchDonorsAtUnspecifiedRegistries()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithRegistries(new List<RegistryCode> {DefaultRegistryCode})
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithFullMatchAtNmdpRegistry.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithFullMatchAtAnthonyNolanRegistry.DonorId);
        } 
        
        [Test]
        public async Task Search_ForAdultDonors_DoesNotMatchCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Adult)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == cordDonorWithFullMatch.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_DoesNotMatchAdultDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusA.DonorId);
        }
        
        [Test]
        public async Task Search_ForCordDonors_MatchesCordDonors()
        {
            var searchCriteria = GetDefaultCriteriaBuilder()
                .WithSearchType(DonorType.Cord)
                .Build();
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == cordDonorWithFullMatch.DonorId);
        }

        /// <returns> An input donor builder pre-populated with default donor data of an exact match. </returns>
        private InputDonorBuilder GetDefaultInputDonorBuilder()
        {
            return new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithRegistryCode(DefaultRegistryCode)
                .WithDonorType(DefaultDonorType)
                .WithHlaAtLocus(
                    Locus.A,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusA_PositionTwo}}
                )
                .WithHlaAtLocus(
                    Locus.B,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusB_PositionTwo}}
                )
                .WithHlaAtLocus(
                    Locus.Drb1,
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusDRB1_PositionOne}},
                    new ExpandedHla {PGroups = new List<string> {PatientPGroup_LocusDRB1_PositionTwo}}
                );
        }
        
        /// <returns> A criteria builder pre-populated with default criteria data of an exact search. </returns>
        private static AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
                .WithSearchType(DefaultDonorType)
                .WithRegistries(new List<RegistryCode>{DefaultRegistryCode})
                .WithDonorMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionOne },
                    PGroupsToMatchInPositionTwo = new List<string> { PatientPGroup_LocusA_BothPositions, PatientPGroup_LocusA_PositionTwo }
                })
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { PatientPGroup_LocusB_PositionOne },
                    PGroupsToMatchInPositionTwo = new List<string> { PatientPGroup_LocusB_PositionTwo }
                })
                .WithLocusMatchCriteria(Locus.Drb1, new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = new List<string> { PatientPGroup_LocusDRB1_PositionOne },
                    PGroupsToMatchInPositionTwo = new List<string> { PatientPGroup_LocusDRB1_PositionTwo }
                })
                .WithTotalMismatchCount(0);
        }
    }
}
