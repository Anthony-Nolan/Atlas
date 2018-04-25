using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.Utils.TestUtils.Assertions;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [TestFixture]
    public class MatchingTests : IntegrationTestBase
    {
        private readonly DonorMatchCriteria searchCriteria = new DonorMatchCriteria
        {
            SearchType = SearchType.Adult,
            RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
            DonorMismatchCountTier1 = 0,
            DonorMismatchCountTier2 = 0,
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

        private IDonorMatchRepository searchRepo;

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            searchRepo = container.Resolve<IDonorMatchRepository>();

            // potential 2/2 homozygous match at locus A
            searchRepo.InsertDonor(new SearchableDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = "Adult",
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
            searchRepo.InsertDonor(new SearchableDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = "Adult",
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
    }
}
