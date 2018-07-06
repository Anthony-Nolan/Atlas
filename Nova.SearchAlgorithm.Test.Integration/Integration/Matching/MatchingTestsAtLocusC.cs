using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Integration.Integration.Matching
{
    public class MatchingTestsAtLocusC : IntegrationTestBase
    {
        private AlleleLevelMatchCriteria searchCriteria;

        private IDonorMatchingService matchingService;
        private InputDonor donorWithFullHomozygousMatchAtLocusC;
        private InputDonor donorWithFullExactHeterozygousMatchAtLocusC;
        private InputDonor donorWithFullCrossHeterozygousMatchAtLocusC;
        private InputDonor donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusC;
        private InputDonor donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusC;
        private InputDonor donorWithNoMatchAtLocusC;

        private readonly List<string> pGroupsAtA = new List<string> {"01:02P"};
        private readonly List<string> pGroupsAtB = new List<string> {"07:02P"};
        private readonly List<string> pGroupsAtDrb = new List<string> {"01:11P"};

        private const string PatientPGroupAtBothPositions = "03:23";
        private const string PatientPGroupAtPositionOne = "07:05";
        private const string PatientPGroupAtPositionTwo = "04:12";

        public MatchingTestsAtLocusC(DonorStorageImplementation param) : base(param)
        {
        }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = container.Resolve<IDonorMatchingService>();
        }

        /// <summary>
        /// Set up test data. This test suite is only testing variations on Locus B - the other required loci should be assumed to always match
        /// </summary>
        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = container.Resolve<IDonorImportRepository>();
            donorWithFullHomozygousMatchAtLocusC = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla {PGroups = pGroupsAtA},
                    A_2 = new ExpandedHla {PGroups = pGroupsAtA},
                    B_1 = new ExpandedHla {PGroups = pGroupsAtB},
                    B_2 = new ExpandedHla {PGroups = pGroupsAtB},
                    DRB1_1 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    DRB1_2 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    C_1 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions}},
                    C_2 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions}}
                }
            };

            importRepo.AddOrUpdateDonorWithHla(donorWithFullHomozygousMatchAtLocusC).Wait();

            donorWithFullExactHeterozygousMatchAtLocusC = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 2,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla {PGroups = pGroupsAtA},
                    A_2 = new ExpandedHla {PGroups = pGroupsAtA},
                    B_1 = new ExpandedHla {PGroups = pGroupsAtB},
                    B_2 = new ExpandedHla {PGroups = pGroupsAtB},
                    DRB1_1 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    DRB1_2 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    C_1 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne}},
                    C_2 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionTwo}}
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullExactHeterozygousMatchAtLocusC).Wait();

            donorWithFullCrossHeterozygousMatchAtLocusC = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 3,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla {PGroups = pGroupsAtA},
                    A_2 = new ExpandedHla {PGroups = pGroupsAtA},
                    B_1 = new ExpandedHla {PGroups = pGroupsAtB},
                    B_2 = new ExpandedHla {PGroups = pGroupsAtB},
                    DRB1_1 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    DRB1_2 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    C_1 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionTwo}},
                    C_2 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne}}
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithFullCrossHeterozygousMatchAtLocusC).Wait();

            donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusC = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 4,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla {PGroups = pGroupsAtA},
                    A_2 = new ExpandedHla {PGroups = pGroupsAtA},
                    B_1 = new ExpandedHla {PGroups = pGroupsAtB},
                    B_2 = new ExpandedHla {PGroups = pGroupsAtB},
                    DRB1_1 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    DRB1_2 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    C_1 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtBothPositions}},
                    C_2 = new ExpandedHla {PGroups = new List<string> {"01:15"}}
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusC).Wait();

            donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusC = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 5,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla {PGroups = pGroupsAtA},
                    A_2 = new ExpandedHla {PGroups = pGroupsAtA},
                    B_1 = new ExpandedHla {PGroups = pGroupsAtB},
                    B_2 = new ExpandedHla {PGroups = pGroupsAtB},
                    DRB1_1 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    DRB1_2 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    C_1 = new ExpandedHla {PGroups = new List<string> {PatientPGroupAtPositionOne, PatientPGroupAtPositionTwo}},
                    C_2 = new ExpandedHla {PGroups = new List<string> {"01:15"}}
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusC).Wait();

            donorWithNoMatchAtLocusC = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 6,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla {PGroups = pGroupsAtA},
                    A_2 = new ExpandedHla {PGroups = pGroupsAtA},
                    B_1 = new ExpandedHla {PGroups = pGroupsAtB},
                    B_2 = new ExpandedHla {PGroups = pGroupsAtB},
                    DRB1_1 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    DRB1_2 = new ExpandedHla {PGroups = pGroupsAtDrb},
                    C_1 = new ExpandedHla {PGroups = new List<string> {"01:98"}},
                    C_2 = new ExpandedHla {PGroups = new List<string> {"01:11"}}
                }
            };
            importRepo.AddOrUpdateDonorWithHla(donorWithNoMatchAtLocusC).Wait();
        }

        [SetUp]
        public void ResetSearchCriteria()
        {
            searchCriteria = new AlleleLevelMatchCriteria
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> {RegistryCode.AN},
                DonorMismatchCount = 0,
                LocusMismatchA = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = pGroupsAtA,
                    HlaNamesToMatchInPositionTwo = pGroupsAtA
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
                },
                LocusMismatchC = new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionOne},
                    HlaNamesToMatchInPositionTwo = new List<string> {PatientPGroupAtBothPositions, PatientPGroupAtPositionTwo}
                },
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHomozygousMatchesAtLocusC()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_MatchesDonorWithTwoOfTwoHeterozygousMatchesAtLocusC()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusC.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullCrossHeterozygousMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_DoesNotMatchDonorsWithFewerThanTwoMatchesAtLocusC()
        {
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusC.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusC.DonorId);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_MatchesDonorsWithTwoMatches()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchC.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusC.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_MatchesDonorWithOneOfTwoHvGAtLocusC()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchC.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_MatchesDonorWithOneOfTwoBothDirectionsAtLocusC()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchC.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtDrb1_DoesNotMatchDonorsWithNoMatchAtLocusC()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchC.MismatchCount = 1;
            var results = await matchingService.Search(searchCriteria);
            results.Should().NotContain(d => d.Donor.DonorId == donorWithNoMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtDrb1_MatchesDonorWithNoMatchAtLocusC()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchC.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithNoMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtDrb1_MatchesDonorsWithExactMatchAtLocusC()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchC.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullExactHeterozygousMatchAtLocusC.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithFullHomozygousMatchAtLocusC.DonorId);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtDrb1_MatchesDonorsWithSingleMatchAtLocusC()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchC.MismatchCount = 2;
            var results = await matchingService.Search(searchCriteria);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInHvGDirectionAndFullMatchInGvHAtLocusC.DonorId);
            results.Should().Contain(d => d.Donor.DonorId == donorWithHalfMatchInBothHvGAndGvHDirectionsAtLocusC.DonorId);
        }
    }
}