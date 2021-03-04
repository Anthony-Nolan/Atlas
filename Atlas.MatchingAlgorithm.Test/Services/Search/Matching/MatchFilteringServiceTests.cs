using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
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
            var match = new MatchResult(default)
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
            var match = new MatchResult(default)
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
            var match = new MatchResult(default);
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, LocusMatchDetailsBuilder.New.WithDoubleMatch());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria {MismatchCount = 0})
                .Build();

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithAsManyMismatchesAsSpecified_ReturnsTrue()
        {
            var match = new MatchResult(default);
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, LocusMatchDetailsBuilder.New.WithSingleMatch());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria {MismatchCount = 1})
                .Build();

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithMoreMismatchesThanSpecified_ReturnsFalse()
        {
            var match = new MatchResult(default);
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, LocusMatchDetailsBuilder.New.WithSingleMatch());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(locus, new AlleleLevelLocusMatchCriteria {MismatchCount = 0})
                .Build();

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithMoreMismatchesAtASingleLocusThanSpecifiedOverall_ReturnsFalse()
        {
            var match = new MatchResult(default);
            match.SetMatchDetailsForLocus(Locus.B, LocusMatchDetailsBuilder.New.WithSingleMatch());
            var criteria = new AlleleLevelMatchCriteriaBuilder().WithDonorMismatchCount(0).WithRequiredLociMatchCriteria(2).Build();

            var result = matchFilteringService.FulfilsTotalMatchCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsTotalMatchCriteria_WithMoreMismatchesAcrossMultipleLociThanSpecifiedOverall_ReturnsFalse()
        {
            var match = new MatchResult(default);
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.B, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.Drb1, LocusMatchDetailsBuilder.New.WithSingleMatch());

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
            var match = new MatchResult(default);
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.B, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.Drb1, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.C, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.Dqb1, LocusMatchDetailsBuilder.New.WithSingleMatch());

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
            var match = new MatchResult(default);
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.B, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.Drb1, LocusMatchDetailsBuilder.New.WithSingleMatch());
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
            var match = new MatchResult(default);
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.B, LocusMatchDetailsBuilder.New.WithSingleMatch());
            match.SetMatchDetailsForLocus(Locus.Drb1, LocusMatchDetailsBuilder.New.WithSingleMatch());
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
            var match = new MatchResult(default) {DonorInfo = new DonorInfo {DonorType = donorType}};
            var criteria = new AlleleLevelMatchCriteria {SearchType = donorType};

            var result = matchFilteringService.FulfilsSearchTypeCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsSearchTypeCriteria_ForMatchOfUnSpecifiedType_ReturnsFalse()
        {
            const DonorType donorType = DonorType.Cord;
            const DonorType searchType = DonorType.Adult;
            var match = new MatchResult(default) {DonorInfo = new DonorInfo {DonorType = donorType}};
            var criteria = new AlleleLevelMatchCriteria {SearchType = searchType};

            var result = matchFilteringService.FulfilsSearchTypeCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsConfigurableMatchCountCriteria_WithBetterMatchesAllowed_WithExactTotalMismatchCount_ReturnsTrue()
        {
            const Locus locus = Locus.A;
            var match = new MatchResult(default) {DonorInfo = new DonorInfo()};
            match.SetMatchDetailsForLocus(locus, LocusMatchDetailsBuilder.New.WithSingleMatch());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithShouldIncludeBetterMatches(true)
                .WithDonorMismatchCount(1)
                .WithLocusMismatchCount(locus, 1)
                .Build();

            var result = matchFilteringService.FulfilsConfigurableMatchCountCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsConfigurableMatchCountCriteria_WithBetterMatchesAllowed_WithFewerMismatchesThanTotalMismatchCount_ReturnsTrue()
        {
            var match = new MatchResult(default) {DonorInfo = new DonorInfo()};
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.Build());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithShouldIncludeBetterMatches(true)
                .WithDonorMismatchCount(1)
                .WithLocusMismatchCount(Locus.A, 1)
                .Build();

            var result = matchFilteringService.FulfilsConfigurableMatchCountCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsConfigurableMatchCountCriteria_WithBetterMatchesDisallowed_WithExactTotalMismatchCount_ReturnsTrue()
        {
            var match = new MatchResult(default) {DonorInfo = new DonorInfo()};
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.WithSingleMatch());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithShouldIncludeBetterMatches(false)
                .WithDonorMismatchCount(1)
                .WithLocusMismatchCount(Locus.A, 1)
                .Build();

            var result = matchFilteringService.FulfilsConfigurableMatchCountCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsConfigurableMatchCountCriteria_WithBetterMatchesDisallowed_WithFewerMismatchesThanTotalMismatchCount_ReturnsFalse()
        {
            var match = new MatchResult(default) {DonorInfo = new DonorInfo()};
            match.SetMatchDetailsForLocus(Locus.A, LocusMatchDetailsBuilder.New.Build());
            var criteria = new AlleleLevelMatchCriteriaBuilder()
                .WithShouldIncludeBetterMatches(false)
                .WithDonorMismatchCount(1)
                .WithLocusMismatchCount(Locus.A, 1)
                .Build();

            var result = matchFilteringService.FulfilsConfigurableMatchCountCriteria(match, criteria);

            result.Should().BeFalse();
        }
    }
}