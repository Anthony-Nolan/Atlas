using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Repositories.Donors;
using Nova.SearchAlgorithm.Repositories.Hla;
using Nova.SearchAlgorithm.Services;
using Nova.SearchAlgorithm.Test.Builders;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Service
{
    [TestFixture]
    public class SearchServiceTests : TestBase<SearchService>
    {
        private const string HlaNameA1 = "a1";
        private const string HlaNameA2 = "a2";
        private const string PGroupA1 = "p1";
        private const string PGroupA1_alternative = "p1a";
        private const string PGroupA2 = "p2";

        private readonly SearchableDonor exactMatch = new SearchableDonor { DonorId = 1 };
        private readonly SearchableDonor bothPositionsMatchGroupOne = new SearchableDonor { DonorId = 2 };
        private readonly SearchableDonor bothGroupsMatchPositionOne = new SearchableDonor { DonorId = 3 };

        private SearchService service;

        [SetUp]
        public void setup()
        {
            service = new SearchService(GetFake<IDonorRepository>(), GetFake<IHlaRepository>());

            GetFake<IHlaRepository>().RetrieveHlaMatches("A", HlaNameA1).Returns(new MatchingHla
            {
                MatchingProteinGroups = new List<string> { PGroupA1, PGroupA1_alternative },
                MatchingSerologyNames = new List<string>()
            });
            GetFake<IHlaRepository>().RetrieveHlaMatches("A", HlaNameA2).Returns(new MatchingHla
            {
                MatchingProteinGroups = new List<string> { PGroupA2 },
                MatchingSerologyNames = new List<string>()
            });

            GetFake<IDonorRepository>().GetDonorMatchesAtLocus(SearchType.Adult, Arg.Any<IEnumerable<RegistryCode>>(), "A", Arg.Any<LocusSearchCriteria>()).Returns(new List<HlaMatch>
            {
                HlaMatchFor("A", TypePositions.One, TypePositions.One, exactMatch, PGroupA1),
                HlaMatchFor("A", TypePositions.Two, TypePositions.Two, exactMatch, PGroupA2),

                HlaMatchFor("A", TypePositions.One, TypePositions.One, bothPositionsMatchGroupOne, PGroupA1),
                HlaMatchFor("A", TypePositions.One, TypePositions.Two, bothPositionsMatchGroupOne, PGroupA1),

                HlaMatchFor("A", TypePositions.One, TypePositions.One, bothGroupsMatchPositionOne, PGroupA1),
                HlaMatchFor("A", TypePositions.Two, TypePositions.One, bothGroupsMatchPositionOne, PGroupA2),
            });

            GetFake<IDonorRepository>().GetDonor(exactMatch.DonorId).Returns(exactMatch);
            GetFake<IDonorRepository>().GetDonor(bothPositionsMatchGroupOne.DonorId).Returns(bothPositionsMatchGroupOne);
            GetFake<IDonorRepository>().GetDonor(bothGroupsMatchPositionOne.DonorId).Returns(bothGroupsMatchPositionOne);
        }

        private HlaMatch HlaMatchFor(string locus, TypePositions searchPosition, TypePositions matchPosition, SearchableDonor donor, string hlaMatchName)
        {
            return new HlaMatch
            {
                DonorId = donor.DonorId,
                SearchTypePosition = searchPosition,
                MatchingTypePositions = matchPosition,
                Locus = locus,
                Name = hlaMatchName
            };
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenTwoMatchesRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 0).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == exactMatch.DonorId);
            results.Where(d => d.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(2);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 0).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().NotContain(d => d.DonorId == bothPositionsMatchGroupOne.DonorId);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenTwoMatchesRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 0).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().NotContain(d => d.DonorId == bothGroupsMatchPositionOne.DonorId);
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenOneMatchRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 1).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == exactMatch.DonorId);
            results.Where(d => d.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(2);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenOneMatchRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 1).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenOneMatchRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 1).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }

        [Test]
        public void ExactMatchDonorIsReturnedWhenNoMatchRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 2).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == exactMatch.DonorId);
            results.Where(d => d.DonorId == exactMatch.DonorId).First().TotalMatchCount.Should().Be(2);
        }

        [Test]
        public void SingleMatchDonorOnSearchSideIsNotReturnedWhenNoMatchRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 2).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == bothPositionsMatchGroupOne.DonorId);
            results.Where(d => d.DonorId == bothPositionsMatchGroupOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }

        [Test]
        public void SingleMatchDonorOnMatchSideIsNotReturnedWhenNoMatchRequired()
        {
            SearchRequest request = new SearchRequestBuilder().WithLocusMismatchA(HlaNameA1, HlaNameA2, 2).Build();

            IEnumerable<DonorMatch> results = service.Search(request);

            results.Should().Contain(d => d.DonorId == bothGroupsMatchPositionOne.DonorId);
            results.Where(d => d.DonorId == bothGroupsMatchPositionOne.DonorId).First().TotalMatchCount.Should().Be(1);
        }
    }
}