using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration.Matching
{
    public class MatchingTestsAtLocusA : IntegrationTestBase
    {
        private AlleleLevelMatchCriteria searchCriteria;

        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocusA;
        private InputDonor donorWithFullHeterozygousMatchAtLocusA;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA;
        private InputDonor donorWithNoMatchAtLocusA;
        
        private readonly List<string> pGroupsAtB = new List<string> { "07:02P" };
        private readonly List<string> pGroupsAtDrb = new List<string> { "01:11P" };
        
        private const string PatientPGroupAtBothPositions = "01:01P";
        private const string PatientPGroupAtPositionOne = "01:02";
        private const string PatientPGroupAtPositionTwo = "02:01";

        public MatchingTestsAtLocusA(DonorStorageImplementation param) : base(param) { }

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

            donorWithFullHomozygousMatchAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionOne } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions, "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullHomozygousMatchAtLocusA).Wait();

            donorWithFullHeterozygousMatchAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 2,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions, "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionTwo, "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullHeterozygousMatchAtLocusA).Wait();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 3,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtBothPositions, "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA).Wait();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 4,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA).Wait();

            donorWithNoMatchAtLocusA = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 5,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = pGroupsAtB },
                    B_2 = new ExpandedHla { PGroups = pGroupsAtB },
                    DRB1_1 = new ExpandedHla { PGroups = pGroupsAtDrb },
                    DRB1_2 = new ExpandedHla { PGroups = pGroupsAtDrb }
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocusA).Wait();
        }

        [SetUp]
        public void ResetSearchCriteria()
        {
            searchCriteria = new AlleleLevelMatchCriteria
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                DonorMismatchCount = 0,
                LocusMismatchA = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionOne },
                    HlaNamesToMatchInPositionTwo = new List<string> { PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo }
                },
                LocusMismatchB = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = pGroupsAtB,
                    HlaNamesToMatchInPositionTwo = pGroupsAtB
                },
                LocusMismatchDRB1 = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = pGroupsAtDrb,
                    HlaNamesToMatchInPositionTwo = pGroupsAtDrb
                }
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }
        
        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocusA()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocusA()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHeterozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtA()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorsWithTwoMatches()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorWithOneOfTwoHvGAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_MatchesDonorWithOneOfTwoBothDirectionsAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtA_DoesNotMatchDonorsWithNoMatchAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_MatchesDonorWithNoMatchAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_MatchesDonorsWithExactMatchAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHeterozygousMatchAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusA.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtA_MatchesDonorsWithSingleMatchAtLocusA()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusA.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusA.DonorId);
        }
    }
}
