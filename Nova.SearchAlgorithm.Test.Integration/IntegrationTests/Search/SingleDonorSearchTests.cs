using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Search
{
    public class SingleDonorSearchTests : IntegrationTestBase
    {
        private ISearchService searchService;
        private InputDonor donor;
        // A selection of valid hla data for the single donor to have
        private readonly PhenotypeInfo<string> donorHlas = new PhenotypeInfo<string>
        {
            A_1 = "01:02",
            A_2 = "01:02",
            B_1 = "14:53",
            B_2 = "14:47",
            DRB1_1 = "13:03:01",
            DRB1_2 = "13:02:01:03",
        };
        // A selection of valid hla strings that do not match the donor's
        private PhenotypeInfo<string> nonMatchingHlas= new PhenotypeInfo<string>
        {
            A_1 = "01:01:01:02N",
            A_2 = "01:01:01:02N",
            B_1 = "07:02:01:01",
            B_2 = "07:02:13",
            DRB1_1 = "14:190",
            DRB1_2 = "14:190",
        };

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            var lookupService = container.Resolve<IHlaMatchingLookupService>();
            var donorRepository = container.Resolve<IDonorImportRepository>();
            
            donor = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = (donorHlas).Map((l, p, h) => h == null ? null : lookupService.GetHlaLookupResult(l.ToMatchLocus(), h).Result.ToExpandedHla(h))
            };
            donorRepository.AddOrUpdateDonorWithHla(donor).Wait();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = container.Resolve<ISearchService>();
        }

        [Test]
        public async Task Search_SixOutOfSix_ExactMatch_ReturnsDonor()
        {
            var searchRequest = new SingleDonorSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().Contain(d => d.DonorId == donor.DonorId);
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusA_DoesNotReturnDonor()
        {
            var searchRequest = new SingleDonorSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.A)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusB_DoesNotReturnDonor()
        {
            var searchRequest = new SingleDonorSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.B)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusDrb1_DoesNotReturnDonor()
        {
            var searchRequest = new SingleDonorSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.Drb1)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }
        
        [Test]
        public async Task Search_SixOutOfSix_MismatchAtMultipleLoci_DoesNotReturnDonor()
        {
            var searchRequest = new SingleDonorSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.A)
                .WithSingleMismatchAt(Locus.B)
                .WithSingleMismatchAt(Locus.Drb1)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }
        
        private class SingleDonorSearchRequestBuilder
        {
            private readonly PhenotypeInfo<string> nonMatchingHlas;
            private SearchRequestBuilder searchRequestBuilder;
            
            public SingleDonorSearchRequestBuilder(PhenotypeInfo<string> donorHlas, PhenotypeInfo<string> nonMatchingHlas)
            {
                this.nonMatchingHlas = nonMatchingHlas;
                searchRequestBuilder = new SearchRequestBuilder()
                    .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                    {
                        MismatchCount = 0
                    })
                    .WithLocusMatchHla(Locus.A, TypePositions.One, donorHlas.A_1)
                    .WithLocusMatchHla(Locus.A, TypePositions.Two, donorHlas.A_2)
                    .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                    {
                        MismatchCount = 0
                    })
                    .WithLocusMatchHla(Locus.B, TypePositions.One, donorHlas.B_1)
                    .WithLocusMatchHla(Locus.B, TypePositions.Two, donorHlas.B_2)
                    .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                    {
                        MismatchCount = 0
                    })
                    .WithLocusMatchHla(Locus.Drb1, TypePositions.One, donorHlas.DRB1_1)
                    .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, donorHlas.DRB1_2);
            }

            public SingleDonorSearchRequestBuilder SixOutOfSix()
            {
                searchRequestBuilder = searchRequestBuilder
                    .WithTotalMismatchCount(0)
                    .WithLocusMatchCount(Locus.A, 0)
                    .WithLocusMatchCount(Locus.B, 0)
                    .WithLocusMatchCount(Locus.Drb1, 0);
                return this;
            }

            public SingleDonorSearchRequestBuilder WithSingleMismatchAt(Locus locus)
            {
                searchRequestBuilder = searchRequestBuilder
                    .WithLocusMatchHla(locus, TypePositions.One, nonMatchingHlas.DataAtLocus(locus).Item1);
                return this;
            }

            public SearchRequest Build()
            {
                return searchRequestBuilder.Build();
            }
        }
    }
}
