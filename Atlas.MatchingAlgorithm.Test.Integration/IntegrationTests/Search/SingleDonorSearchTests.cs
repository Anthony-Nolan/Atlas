using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Services.Donors;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    public class SingleDonorSearchTests
    {
        private ISearchService searchService;

        private DonorInfoWithExpandedHla donor;

        // A selection of valid hla data for the single donor to have
        private readonly PhenotypeInfo<string> donorHlas = new PhenotypeInfo<string>
        {
            A =
            {
                Position1 = "01:02",
                Position2 = "01:02",
            },
            B =
            {
                Position1 = "14:53",
                Position2 = "14:47",
            },
            Drb1 =
            {
                Position1 = "13:03:01",
                Position2 = "13:02:01:03",
            },
            C =
            {
                Position1 = "02:02",
                Position2 = "02:02",
            }
        };

        // A selection of valid hla strings that do not match the donor's
        private readonly PhenotypeInfo<string> nonMatchingHlas = new PhenotypeInfo<string>
        {
            A =
            {
                Position1 = "02:01:01:01",
                Position2 = "02:01:01:01",
            },
            B =
            {
                Position1 = "07:02:01:01",
                Position2 = "07:02:13",
            },
            Drb1 =
            {
                Position1 = "14:190",
                Position2 = "14:190",
            },
            C =
            {
                Position1 = "07:01",
                Position2 = "07:01",
            }
        };

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            var donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>().BuildForActiveHlaNomenclatureVersion();
            var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo { HlaNames = donorHlas }).Result.MatchingHla;
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorRepository = repositoryFactory.GetDonorUpdateRepository();

            donor = new DonorInfoWithExpandedHla
            {
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                HlaNames = donorHlas,
                MatchingHla = matchingHlaPhenotype
            };
            donorRepository.InsertBatchOfDonorsWithExpandedHla(new []{donor}).Wait();
        }

        [SetUp]
        public void ResolveSearchService()
        {
            searchService = DependencyInjection.DependencyInjection.Provider.GetService<ISearchService>();
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
        public async Task Search_SixOutOfSix_LociExcludedFromSearchHaveMatchCounts()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // C & DQB1 should both be populated from scoring data in a 6/6 only search
            result?.SearchResultAtLocusC.MatchCount.Should().Be(2);
            result?.SearchResultAtLocusDqb1.MatchCount.Should().Be(2);
        }

        [Test]
        public async Task Search_SixOutOfSix_LociExcludedFromSearchAreNotIncludedInTotalMatchCount()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            result?.TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task Search_SixOutOfSix_TypedLociExcludedFromSearchSetIsLocusTypedAsTrue()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.DonorId == donor.DonorId);

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
            var result = results.SingleOrDefault(d => d.DonorId == donor.DonorId);

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
            var result = results.SingleOrDefault(d => d.DonorId == donor.DonorId);

            // DQB1 is not typed and not included in search
            result?.SearchResultAtLocusDqb1.IsLocusTyped.Should().BeFalse();
        }
    }
}