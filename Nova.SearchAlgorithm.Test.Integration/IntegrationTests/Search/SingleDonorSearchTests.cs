using System.Linq;
using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Extensions.MatchingDictionaryConversionExtensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.MatchingDictionaryConversions;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Threading.Tasks;

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
        private readonly PhenotypeInfo<string> nonMatchingHlas = new PhenotypeInfo<string>
        {
            A_1 = "01:01:01:02N",
            A_2 = "01:01:01:02N",
            B_1 = "07:02:01:01",
            B_2 = "07:02:13",
            DRB1_1 = "14:190",
            DRB1_2 = "14:190"
        };

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            var lookupService = Container.Resolve<IHlaMatchingLookupService>();
            var donorRepository = Container.Resolve<IDonorImportRepository>();
            
            donor = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = donorHlas.Map((l, p, h) => h == null ? null : lookupService.GetHlaLookupResult(l.ToMatchLocus(), h).Result.ToExpandedHla(h))
            };
            donorRepository.AddOrUpdateDonorWithHla(donor).Wait();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = Container.Resolve<ISearchService>();
        }

        [Test]
        public async Task Search_SixOutOfSix_ExactMatch_ReturnsDonor()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().Contain(d => d.DonorId == donor.DonorId);
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusA_DoesNotReturnDonor()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.A)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusB_DoesNotReturnDonor()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.B)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusDrb1_DoesNotReturnDonor()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.Drb1)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }
        
        [Test]
        public async Task Search_SixOutOfSix_MismatchAtMultipleLoci_DoesNotReturnDonor()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithSingleMismatchAt(Locus.A)
                .WithSingleMismatchAt(Locus.B)
                .WithSingleMismatchAt(Locus.Drb1)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_LociExcludedFromSearch_HaveNullMatchCounts()
        {
            var searchRequest = new ThreeLocusSearchRequestBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result =  results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // C & DQB1 should both be null in a 6/6 only search
            result?.SearchResultAtLocusC.MatchCount.Should().BeNull();
            result?.SearchResultAtLocusDqb1.MatchCount.Should().BeNull();
        }
    }
}
