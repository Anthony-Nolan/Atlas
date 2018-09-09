using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Linq;
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
            C_1 = "02:02",
            C_2 = "02:02",
        };
        // A selection of valid hla strings that do not match the donor's
        private readonly PhenotypeInfo<string> nonMatchingHlas = new PhenotypeInfo<string>
        {
            A_1 = "01:01:01:02N",
            A_2 = "01:01:01:02N",
            B_1 = "07:02:01:01",
            B_2 = "07:02:13",
            DRB1_1 = "14:190",
            DRB1_2 = "14:190",
            C_1 = "07:01",
            C_2 = "07:01",
        };

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            var lookupService = Container.Resolve<ILocusHlaMatchingLookupService>();
            var donorRepository = Container.Resolve<IDonorImportRepository>();

            donor = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = donorHlas.ToExpandedHlaPhenotype(lookupService).Result
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
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().Contain(d => d.DonorId == donor.DonorId);
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusA_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.A)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusB_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.B)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusDrb1_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.Drb1)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }
        
        [Test]
        public async Task Search_SixOutOfSix_MismatchAtMultipleLoci_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.A)
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.B)
                .WithPositionOneOfSearchHlaMismatchedAt(Locus.Drb1)
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_LociExcludedFromSearchHaveNullMatchCounts()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result =  results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // C & DQB1 should both be null in a 6/6 only search
            result?.SearchResultAtLocusC.MatchCount.Should().BeNull();
            result?.SearchResultAtLocusDqb1.MatchCount.Should().BeNull();
        }
        
        [Test]
        public async Task Search_SixOutOfSix_TypedLociExcludedFromSearchSetIsLocusTypedAsTrue()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result =  results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // C is typed but not included in search
            result?.SearchResultAtLocusC.IsLocusTyped.Should().BeTrue();
        }
        
        [Test]
        public async Task Search_SixOutOfSix_TypedLociIncludedInSearchSetIsLocusTypedAsTrue()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result =  results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // A is typed and included in search
            result?.SearchResultAtLocusA.IsLocusTyped.Should().BeTrue();
        }
        
        [Test]
        public async Task Search_SixOutOfSix_UntypedLociExcludedFromSearchSetIsLocusTypedAsFalse()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result =  results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // DQB1 is not typed and not included in search
            result?.SearchResultAtLocusDqb1.IsLocusTyped.Should().BeFalse();
        }
    }
}
