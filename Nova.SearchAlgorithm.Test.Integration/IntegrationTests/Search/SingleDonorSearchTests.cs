using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Search
{
    public class SingleDonorSearchTests : IntegrationTestBase
    {
        private ISearchService searchService;
        private InputDonor donor;

        private const string DonorHlaAtA = "01:02";
        private const string DonorHlaAtB1 = "14:53";
        private const string DonorHlaAtB2 = "14:47";
        private const string DonorHlaAtDrb11 = "13:03:01";
        private const string DonorHlaAtDrb12 = "13:02:01:03";

        public SingleDonorSearchTests(DonorStorageImplementation param) : base(param) { }

        [OneTimeSetUp]
        public void ImportTestDonor()
        {
            var lookupService = container.Resolve<IMatchingDictionaryLookupService>();
            var donorRepository = container.Resolve<IDonorImportRepository>();
            donor = new InputDonor
            {
                RegistryCode = RegistryCode.AN,
                DonorType = DonorType.Adult,
                DonorId = DonorIdGenerator.NextId(),
                MatchingHla = (new PhenotypeInfo<string>
                {
                    A_1 = DonorHlaAtA,
                    A_2 = DonorHlaAtA,
                    B_1 = DonorHlaAtB1,
                    B_2 = DonorHlaAtB2,
                    DRB1_1 = DonorHlaAtDrb11,
                    DRB1_2 = DonorHlaAtDrb12,
                }).Map((l, p, h) => h == null ? null : lookupService.GetMatchingHla(l.ToMatchLocus(), h).Result.ToExpandedHla(h))
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
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtA,
                    SearchHla2 = DonorHlaAtA
                })
                .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtB1,
                    SearchHla2 = DonorHlaAtB2
                })
                .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtDrb11,
                    SearchHla2 = DonorHlaAtDrb12
                })
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().Contain(d => d.DonorId == donor.DonorId);
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusA_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = "01:01:01:02N",
                    SearchHla2 = "01:01:01:02N"
                })
                .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtB1,
                    SearchHla2 = DonorHlaAtB2
                })
                .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtDrb11,
                    SearchHla2 = DonorHlaAtDrb12
                })
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusB_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtA,
                    SearchHla2 = DonorHlaAtA
                })
                .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = "07:02:01:01",
                    SearchHla2 = "07:02:13"
                })
                .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtDrb11,
                    SearchHla2 = DonorHlaAtDrb12
                })
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task Search_SixOutOfSix_SingleMismatchAtLocusDrb1_DoesNotReturnDonor()
        {
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .WithLocusMatchCriteria(Locus.A, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtA,
                    SearchHla2 = DonorHlaAtA
                })
                .WithLocusMatchCriteria(Locus.B, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = DonorHlaAtB1,
                    SearchHla2 = DonorHlaAtB2
                })
                .WithLocusMatchCriteria(Locus.Drb1, new LocusMismatchCriteria
                {
                    MismatchCount = 0,
                    SearchHla1 = "14:190",
                    SearchHla2 = "14:190"
                })
                .Build();
            
            var results = await searchService.Search(searchRequest);

            results.Should().BeEmpty();
        }
    }
}
