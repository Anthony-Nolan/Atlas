using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.TestUtils.Assertions;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Repositories.Hla;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using System.Collections;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class ExactMatchAndMismatchAtLocusATests : IntegrationTestBase
    {
        private ISearchService searchService;
        
        public ExactMatchAndMismatchAtLocusATests(DonorStorageImplementation param) : base(param) { }

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var hlaRepository = container.Resolve<IHlaRepository>();
            var donorRepository = container.Resolve<IDonorMatchRepository>();
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
            IEnumerable<PotentialMatch> results = searchService.Search(new SearchRequest
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
                        SearchHla2 = "14:47"
                    },
                    LocusMismatchDRB1 = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "04:163",
                        SearchHla2 = "01:08"
                    }
                }
            });

            results.Should().Contain(d => d.DonorId == 1);
        }

        [Test]
        public void SixOfSixSingleDonorMismatchAtLocusA()
        {
            IEnumerable<PotentialMatch> results = searchService.Search(new SearchRequest
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
                        SearchHla2 = "14:47"
                    },
                    LocusMismatchDRB1 = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "04:163",
                        SearchHla2 = "01:08"
                    }
                }
            });

            results.Should().BeEmpty();
        }

        [Test]
        public void SixOfSixSingleDonorMismatchAtLocusB()
        {
            IEnumerable<PotentialMatch> results = searchService.Search(new SearchRequest
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
                        SearchHla1 = "07:02:01:01",
                        SearchHla2 = "07:02:13"
                    },
                    LocusMismatchDRB1 = new LocusMismatchCriteria
                    {
                        MismatchCount = 0,
                        SearchHla1 = "04:163",
                        SearchHla2 = "01:08"
                    }
                }
            });

            results.Should().BeEmpty();
        }
    }
}
