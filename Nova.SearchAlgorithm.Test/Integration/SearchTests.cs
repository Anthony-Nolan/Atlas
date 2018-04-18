using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.TestUtils.Assertions;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Hla;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Test.Integration
{
    [TestFixture]
    public class SearchTests : IntegrationTestBase
    {
        private ISearchService searchService;

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var hlaRepository = container.Resolve<IHlaRepository>();
            var donorRepository = container.Resolve<IDonorRepository>();
            donorRepository.InsertDonor(new SearchableDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = "Adult",
                DonorId = 1,
                MatchingHla = hlaRepository.RetrieveHlaMatches(new PhenotypeInfo<string>
                {
                    A_1 = "01:02",
                    A_2 = "01:02",
                    B_1 = "14:53",
                    B_2 = "14:47",
                    DRB1_1 = "04:163",
                    DRB1_2 = "01:08",
                })
            });
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = container.Resolve<ISearchService>();
        }

        [Test]
        public void SixOfSixSingleDonorExactMatch()
        {
            IEnumerable<DonorMatch> results = searchService.Search(new SearchRequest
            {
                SearchType = SearchType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCountTier1 = 0,
                    DonorMismatchCountTier2 = 0,
                    LocusMismatchA = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "01:02",
                        SearchHla2 = "01:02"
                    },
                    LocusMismatchB = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "14:53",
                        SearchHla2 = "14:57"
                    },
                    LocusMismatchDRB1 = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "04:163",
                        SearchHla2 = "01:04"
                    }
                }
            });

            results.Should().Contain(d => d.DonorId == 1);
        }

        [Test]
        public void SixOfSixSingleDonorMismatchAtLocusA()
        {
            IEnumerable<DonorMatch> results = searchService.Search(new SearchRequest
            {
                SearchType = SearchType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCountTier1 = 0,
                    DonorMismatchCountTier2 = 0,
                    LocusMismatchA = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "01:01:01:02N",
                        SearchHla2 = "01:01:01:02N"
                    },
                    LocusMismatchB = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "14:53",
                        SearchHla2 = "14:57"
                    },
                    LocusMismatchDRB1 = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "04:163",
                        SearchHla2 = "01:04"
                    }
                }
            });

            results.Should().BeEmpty();
        }
    }
}
