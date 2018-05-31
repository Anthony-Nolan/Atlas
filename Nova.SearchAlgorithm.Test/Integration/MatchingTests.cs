using System.Collections.Generic;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class MatchingTests : IntegrationTestBase
    {
        private DonorMatchCriteria searchCriteria;

        private IDonorMatchRepository searchRepo;

        public MatchingTests(DonorStorageImplementation param) : base(param) { }
        
        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            searchRepo = container.Resolve<IDonorMatchRepository>();

            // potential 2/2 homozygous match at locus A
            searchRepo.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "01:02" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            });

            // potential 2/2 heterozygous match at locus A
            searchRepo.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 2,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "02:01", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            });

            // potential 1/2 match at locus A - 1/2 in HvG direction, 2/2 in GvH direction
            searchRepo.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 3,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:01P", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            });

            // potential 1/2 match at locus A - 1/2 in both directions
            searchRepo.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 4,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "01:02", "02:01" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            });

            // 0/2 at locus A
            searchRepo.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 5,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "26:04", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "08:01P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            });

            // 0/2 at locus A, 1/2 at locus B
            searchRepo.InsertDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 6,
                MatchingHla = new PhenotypeInfo<ExpandedHla>
                {
                    A_1 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    A_2 = new ExpandedHla { PGroups = new List<string> { "11:59", "30:02P" } },
                    B_1 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    B_2 = new ExpandedHla { PGroups = new List<string> { "07:02P" } },
                    DRB1_1 = new ExpandedHla { PGroups = new List<string> { "01:11P" } },
                    DRB1_2 = new ExpandedHla { PGroups = new List<string> { "03:41P" } }
                }
            });
        }

        [SetUp]
        public void ResetSearchCriteria()
        {
            searchCriteria = new DonorMatchCriteria
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                DonorMismatchCount = 0,
                LocusMismatchA = new DonorLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { "01:01P", "01:02" },
                    HlaNamesToMatchInPositionTwo = new List<string> { "01:01P", "02:01" }
                },
                LocusMismatchB = new DonorLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { "07:02P" },
                    HlaNamesToMatchInPositionTwo = new List<string> { "08:01P" }
                },
                LocusMismatchDRB1 = new DonorLocusMatchCriteria
                {
                    MismatchCount = 0,
                    HlaNamesToMatchInPositionOne = new List<string> { "01:11P" },
                    HlaNamesToMatchInPositionTwo = new List<string> { "03:41P" }
                }
            };
        }

        [Test]
        public void TwoOfTwoHomozygousMatchAtLocusA()
        {
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 1);
        }

        [Test]
        public void TwoOfTwoHeterozygousMatchAtLocusA()
        {
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 2);
        }

        [Test]
        public void OneHalfMatchesNotPresentInExactSearchResults()
        {
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().NotContain(d => d.DonorId == 3);
            results.Should().NotContain(d => d.DonorId == 4);
            results.Should().NotContain(d => d.DonorId == 5);
            results.Should().NotContain(d => d.DonorId == 6);
        }

        [Test]
        public void ExactMatchIsReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 1);
            results.Should().Contain(d => d.DonorId == 2);
        }

        [Test]
        public void OneOfTwoHvGAtLocusAReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 3);
        }

        [Test]
        public void OneOfTwoBothDirectionsAtLocusAReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 4);
        }

        [Test]
        public void NoMatchAtLocusANotReturnedBySearchWithOneMismatch()
        {
            searchCriteria.DonorMismatchCount = 1;
            searchCriteria.LocusMismatchA.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().NotContain(d => d.DonorId == 5);
            results.Should().NotContain(d => d.DonorId == 6);
        }

        [Test]
        public void NoMatchAtLocusAReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 5);
        }

        [Test]
        public void NoMatchAtLocusAHalfAtLocusBNotReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().NotContain(d => d.DonorId == 6);
        }

        [Test]
        public void ExactMatchIsReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 1);
            results.Should().Contain(d => d.DonorId == 2);
        }

        [Test]
        public void HalfMatchAtLocusAReturnedBySearchWithTwoMismatches()
        {
            searchCriteria.DonorMismatchCount = 2;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 3);
            results.Should().Contain(d => d.DonorId == 4);
        }

        [Test]
        public void ExactMatchIsReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 1);
            results.Should().Contain(d => d.DonorId == 2);
        }

        [Test]
        public void HalfMatchAtLocusaReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 3);
            results.Should().Contain(d => d.DonorId == 4);
        }

        [Test]
        public void NoMatchAtLocusABReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 5);
        }

        [Test]
        public void NoMatchAtLocusAHalfAtLocusBReturnedBySearchWithThreeMismatches()
        {
            searchCriteria.DonorMismatchCount = 3;
            searchCriteria.LocusMismatchA.MismatchCount = 2;
            searchCriteria.LocusMismatchB.MismatchCount = 1;
            IEnumerable<PotentialMatch> results = searchRepo.Search(searchCriteria);
            results.Should().Contain(d => d.DonorId == 6);
        }
    }
}
