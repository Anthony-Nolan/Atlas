using System.Collections.Generic;
using NUnit.Framework;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Repositories;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services;

namespace Nova.SearchAlgorithm.Test.Integration
{
    public class ExactMatchAndMismatchAtLocusATests : IntegrationTestBase
    {
        private ISearchService searchService;
        
        public ExactMatchAndMismatchAtLocusATests(DonorStorageImplementation param) : base(param) { }

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            var lookupService = container.Resolve<IMatchingDictionaryLookupService>();
            var donorRepository = container.Resolve<IDonorImportRepository>();
            donorRepository.AddOrUpdateDonor(new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = 1,
                MatchingHla = (new PhenotypeInfo<string>
                {
                    A_1 = "01:02",
                    A_2 = "01:02",
                    B_1 = "14:53",
                    B_2 = "14:47",
                    DRB1_1 = "13:03:01",
                    DRB1_2 = "13:02:01:03",
                }).Map((l, p, h) => lookupService.GetMatchingHla(l.ToMatchLocus(), h).Result.ToExpandedHla())
            }).Wait();
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
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = 0,
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
                        SearchHla1 = "13:03:01",
                        SearchHla2 = "13:02:01:03"
                    }
                }
            }).Result;

            results.Should().Contain(d => d.DonorId == 1);
        }

        [Test]
        public void SixOfSixSingleDonorMismatchAtLocusA()
        {
            IEnumerable<PotentialMatch> results = searchService.Search(new SearchRequest
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = 0,
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
                        SearchHla1 = "13:03:01",
                        SearchHla2 = "13:02:01:03"
                    }
                }
            }).Result;

            results.Should().BeEmpty();
        }

        [Test]
        public void SixOfSixSingleDonorMismatchAtLocusB()
        {
            IEnumerable<PotentialMatch> results = searchService.Search(new SearchRequest
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = 0,
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
                        SearchHla1 = "13:03:01",
                        SearchHla2 = "13:02:01:03"
                    }
                }
            }).Result;

            results.Should().BeEmpty();
        }

        [Test]
        public void SixOfSixSingleDonorMismatchAtLocusDrb1()
        {
            IEnumerable<PotentialMatch> results = searchService.Search(new SearchRequest
            {
                SearchType = DonorType.Adult,
                RegistriesToSearch = new List<RegistryCode> { RegistryCode.AN },
                MatchCriteria = new MismatchCriteria
                {
                    DonorMismatchCount = 0,
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
                        SearchHla1 = "14:190",
                        SearchHla2 = "14:190"
                    }
                }
            }).Result;

            results.Should().BeEmpty();
        }
    }
}
