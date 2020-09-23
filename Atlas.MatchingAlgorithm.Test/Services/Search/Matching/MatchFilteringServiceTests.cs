using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Common.Models.SearchResults;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.SearchResults;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
{
    [TestFixture]
    public class MatchFilteringServiceTests
    {
        private IMatchFilteringService matchFilteringService;

        [SetUp]
        public void SetUp()
        {
            matchFilteringService = new MatchFilteringService();
        }

        [Test]
        public void IsAvailableForSearch_MatchIsAvailableForSearch_ReturnsTrue()
        {
            var match = new MatchResult
            {
                DonorInfo = new DonorInfo
                {
                    IsAvailableForSearch = true
                }
            };

            var result = matchFilteringService.IsAvailableForSearch(match);

            result.Should().BeTrue();
        }

        [Test]
        public void IsAvailableForSearch_MatchIsUnavailableForSearch_ReturnsFalse()
        {
            var match = new MatchResult
            {
                DonorInfo = new DonorInfo
                {
                    IsAvailableForSearch = false
                }
            };

            var result = matchFilteringService.IsAvailableForSearch(match);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithFewerMismatchesThanSpecified_ReturnsTrue()
        {
            var match = new MatchResult();
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 2});
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria {MismatchCount = 0})
                .Build();

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithAsManyMismatchesAsSpecified_ReturnsTrue()
        {
            var match = new MatchResult();
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .Build();

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithMoreMismatchesThanSpecified_ReturnsFalse()
        {
            var match = new MatchResult();
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria {MismatchCount = 0})
                .Build();

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithMoreMismatchesAtASingleLocusThanSpecifiedOverall_ReturnsFalse()
        {
            var match = new MatchResult();
            match.SetMatchDetailsForLocus(Locus.B, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria {DonorMismatchCount = 0};

            var result = matchFilteringService.FulfilsTotalMatchCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithMoreMismatchesAcrossMultipleLociThanSpecifiedOverall_ReturnsFalse()
        {
            var match = new MatchResult();
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.B, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.Drb1, new LocusMatchDetails {MatchCount = 1});
            
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(Locus.A, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .WithLocusMatchCriteria(Locus.Drb1, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .Build();

            var result = matchFilteringService.FulfilsTotalMatchCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithMoreMismatchesAcrossAllLociThanSpecifiedOverall_ReturnsFalse()
        {
            var match = new MatchResult();
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.B, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.Drb1, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.C, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.Dqb1, new LocusMatchDetails {MatchCount = 1});
            
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(2)
                .WithLocusMatchCriteria(Locus.A, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .WithLocusMatchCriteria(Locus.B, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .WithLocusMatchCriteria(Locus.Drb1, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .WithLocusMatchCriteria(Locus.Dqb1, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .WithLocusMatchCriteria(Locus.C, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .Build();

            var result = matchFilteringService.FulfilsTotalMatchCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithFewerTotalMismatchesThanSpecifiedOverall_ReturnsTrue()
        {
            var match = new MatchResult();
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.B, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.Drb1, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria
            {
                DonorMismatchCount = 4,
            };

            var result = matchFilteringService.FulfilsTotalMatchCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithAsManyTotalMismatchesAsSpecifiedOverall_ReturnsTrue()
        {
            var match = new MatchResult();
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.B, new LocusMatchDetails {MatchCount = 1});
            match.SetMatchDetailsForLocus(Locus.Drb1, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria
            {
                DonorMismatchCount = 3,
            };

            var result = matchFilteringService.FulfilsTotalMatchCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsSearchTypeCriteria_ForMatchOfSpecifiedType_ReturnsTrue()
        {
            const DonorType donorType = DonorType.Cord;
            var match = new MatchResult {DonorInfo = new DonorInfo {DonorType = donorType}};
            var criteria = new AlleleLevelMatchCriteria {SearchType = donorType};

            var result = matchFilteringService.FulfilsSearchTypeCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsSearchTypeCriteria_ForMatchOfUnSpecifiedType_ReturnsFalse()
        {
            const DonorType donorType = DonorType.Cord;
            const DonorType searchType = DonorType.Adult;
            var match = new MatchResult {DonorInfo = new DonorInfo {DonorType = donorType}};
            var criteria = new AlleleLevelMatchCriteria {SearchType = searchType};

            var result = matchFilteringService.FulfilsSearchTypeCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsSearchTypeSpecificCriteria_ForAdultSearch_WithExactTotalMismatchCount_ReturnsTrue()
        {
            const DonorType searchType = DonorType.Adult;
            var match = new MatchResult {DonorInfo = new DonorInfo {DonorType = searchType}};
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchType,
                DonorMismatchCount = 1
            };

            var result = matchFilteringService.FulfilsSearchTypeSpecificCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsSearchTypeSpecificCriteria_ForAdultSearch_WithFewerMismatchesThanTotalMismatchCount_ReturnsFalse()
        {
            const DonorType searchType = DonorType.Adult;
            var match = new MatchResult {DonorInfo = new DonorInfo {DonorType = searchType}};
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 0});
            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchType,
                DonorMismatchCount = 1
            };

            var result = matchFilteringService.FulfilsSearchTypeSpecificCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsSearchTypeSpecificCriteria_ForCordSearch_WithExactTotalMismatchCount_ReturnsTrue()
        {
            const DonorType searchType = DonorType.Cord;
            var match = new MatchResult {DonorInfo = new DonorInfo {DonorType = searchType}};
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchType,
                DonorMismatchCount = 1
            };

            var result = matchFilteringService.FulfilsSearchTypeSpecificCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsSearchTypeSpecificCriteria_ForCordSearch_WithFewerMismatchesThanTotalMismatchCount_ReturnsTrue()
        {
            const DonorType searchType = DonorType.Cord;
            var match = new MatchResult {DonorInfo = new DonorInfo {DonorType = searchType}};
            match.SetMatchDetailsForLocus(Locus.A, new LocusMatchDetails {MatchCount = 0});
            var criteria = new AlleleLevelMatchCriteria
            {
                SearchType = searchType,
                DonorMismatchCount = 1
            };

            var result = matchFilteringService.FulfilsSearchTypeSpecificCriteria(match, criteria);

            result.Should().BeTrue();
        }
    }
}