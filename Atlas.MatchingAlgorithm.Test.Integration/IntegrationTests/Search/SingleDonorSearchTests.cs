using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Search
{
    public class SingleDonorSearchTests
    {
        private ISearchService searchService;

        private DonorInfoWithExpandedHla donor;

        // A selection of valid hla data for the single donor to have
        private readonly PhenotypeInfo<string> donorHlas = new PhenotypeInfo<string>
        (
            valueA: new LocusInfo<string>("01:02", "01:02"),
            valueB: new LocusInfo<string>("14:53", "14:47"),
            valueDrb1: new LocusInfo<string>("13:03:01", "13:02:01:03"),
            valueC: new LocusInfo<string>("02:02", "02:02")
        );

        // A selection of valid hla strings that do not match the donor's
        private readonly PhenotypeInfo<string> nonMatchingHlas = new PhenotypeInfo<string>
        (
            valueA: new LocusInfo<string>("02:01:01:01", "02:01:01:01"),
            valueB: new LocusInfo<string>("07:02:01:01", "07:02:13"),
            valueDrb1: new LocusInfo<string>("14:190", "14:190"),
            valueC: new LocusInfo<string>("07:01", "07:01")
        );

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>()
                    .BuildForActiveHlaNomenclatureVersion();
                var matchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo {HlaNames = donorHlas}).Result.MatchingHla;
                var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
                var donorRepository = repositoryFactory.GetDonorUpdateRepository();

                donor = new DonorInfoWithExpandedHla
                {
                    DonorType = DonorType.Adult,
                    DonorId = DonorIdGenerator.NextId(),
                    HlaNames = donorHlas,
                    MatchingHla = matchingHlaPhenotype
                };
                donorRepository.InsertBatchOfDonorsWithExpandedHla(new[] {donor}, false).Wait();
            });
        }

        [SetUp]
        public void SetUp()
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

            results.Should().Contain(d => d.AtlasDonorId == donor.DonorId);
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
        public async Task Search_SixOutOfSix_LociExcludedFromMatchingButIncludedInScoring_ReturnsMatchCounts()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithLociToScore(new List<Locus> {Locus.C, Locus.Dqb1})
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == donor.DonorId);

            result?.ScoringResult.ScoringResultsByLocus.C.MatchCount.Should().Be(2);
            result?.ScoringResult.ScoringResultsByLocus.Dqb1.MatchCount.Should().Be(2);
        }

        [Test]
        public async Task Search_SixOutOfSix_LociExcludedFromMatchingButIncludedInScoring_MatchingResultTotalMatchCountOnlyConsidersMatchingLoci()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithLociToScore(new List<Locus> {Locus.C, Locus.Dqb1})
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == donor.DonorId);

            // 3 match loci (A, B, DRB1) x 2
            result?.MatchingResult.TotalMatchCount.Should().Be(6);
        }

        [Test]
        public async Task Search_SixOutOfSix_TypedLociExcludedFromMatchingButIncludedInScoring_ReturnsIsLocusTypedAsTrue()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithLociToScore(new List<Locus> {Locus.C})
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == donor.DonorId);

            // C is typed but not included in matching
            result?.ScoringResult.ScoringResultsByLocus.C.IsLocusTyped.Should().BeTrue();
        }

        [Test]
        public async Task Search_SixOutOfSix_TypedLociIncludedInBothMatchingAndScoring_ReturnsIsLocusTypedAsTrue()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithLociToScore(new List<Locus> {Locus.A})
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == donor.DonorId);

            // A is typed and included in search
            result?.ScoringResult.ScoringResultsByLocus.A.IsLocusTyped.Should().BeTrue();
        }

        [Test]
        public async Task Search_SixOutOfSix_UntypedLociExcludedFromMatchingButIncludedInScoring_ReturnsIsLocusTypedAsFalse()
        {
            var searchRequest = new SearchRequestFromHlasBuilder(donorHlas, nonMatchingHlas)
                .SixOutOfSix()
                .WithLociToScore(new List<Locus> {Locus.Dqb1})
                .Build();

            var results = await searchService.Search(searchRequest);
            var result = results.SingleOrDefault(d => d.AtlasDonorId == donor.DonorId);

            // DQB1 is not typed and not included in search
            result?.ScoringResult.ScoringResultsByLocus.Dqb1.IsLocusTyped.Should().BeFalse();
        }
    }
}