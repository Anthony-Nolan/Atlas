using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.SearchResults;
using Nova.SearchAlgorithm.Services.Matching;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Matching
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
        public void FulfilsPerLocusMatchCriteria_WithFewerMismatchesThanSpecified_ReturnsTrue()
        {
            var match = new MatchResult();
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 2});
            var criteria = new AlleleLevelMatchCriteria {LocusMismatchB = new AlleleLevelLocusMatchCriteria {MismatchCount = 0}};

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithAsManyMismatchesAsSpecified_ReturnsTrue()
        {
            var match = new MatchResult();
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria {LocusMismatchB = new AlleleLevelLocusMatchCriteria {MismatchCount = 1}};

            var result = matchFilteringService.FulfilsPerLocusMatchCriteria(match, criteria, locus);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsPerLocusMatchCriteria_WithMoreMismatchesThanSpecified_ReturnsFalse()
        {
            var match = new MatchResult();
            const Locus locus = Locus.B;
            match.SetMatchDetailsForLocus(locus, new LocusMatchDetails {MatchCount = 1});
            var criteria = new AlleleLevelMatchCriteria {LocusMismatchB = new AlleleLevelLocusMatchCriteria {MismatchCount = 0}};

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
            var criteria = new AlleleLevelMatchCriteria
            {
                DonorMismatchCount = 2,
                LocusMismatchA = new AlleleLevelLocusMatchCriteria {MismatchCount = 1},
                LocusMismatchB = new AlleleLevelLocusMatchCriteria {MismatchCount = 1},
                LocusMismatchDrb1 = new AlleleLevelLocusMatchCriteria {MismatchCount = 1}
            };

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
            var criteria = new AlleleLevelMatchCriteria
            {
                DonorMismatchCount = 2,
                LocusMismatchA = new AlleleLevelLocusMatchCriteria {MismatchCount = 1},
                LocusMismatchB = new AlleleLevelLocusMatchCriteria {MismatchCount = 1},
                LocusMismatchDrb1 = new AlleleLevelLocusMatchCriteria {MismatchCount = 1},
                LocusMismatchC = new AlleleLevelLocusMatchCriteria {MismatchCount = 1},
                LocusMismatchDqb1 = new AlleleLevelLocusMatchCriteria {MismatchCount = 1}
            };

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
        public void FulfilsRegistryCriteria_ForMatchAtSpecifiedRegistry_ReturnsTrue()
        {
            const RegistryCode specifiedRegistry = RegistryCode.AN;
            var match = new MatchResult {Donor = new DonorResult {RegistryCode = specifiedRegistry}};
            var criteria = new AlleleLevelMatchCriteria {RegistriesToSearch = new List<RegistryCode> {specifiedRegistry}};

            var result = matchFilteringService.FulfilsRegistryCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsRegistryCriteria_ForMatchAtOneOfMultipleSpecifiedRegistries_ReturnsTrue()
        {
            const RegistryCode patientRegistry = RegistryCode.DKMS;
            var match = new MatchResult {Donor = new DonorResult {RegistryCode = patientRegistry}};
            var criteria = new AlleleLevelMatchCriteria {RegistriesToSearch = new List<RegistryCode> {patientRegistry, RegistryCode.FRANCE}};

            var result = matchFilteringService.FulfilsRegistryCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsRegistryCriteria_ForMatchAtUnspecifiedRegistry_ReturnsFalse()
        {
            const RegistryCode patientRegistry = RegistryCode.DKMS;
            var match = new MatchResult {Donor = new DonorResult {RegistryCode = patientRegistry}};
            var criteria = new AlleleLevelMatchCriteria {RegistriesToSearch = new List<RegistryCode> {RegistryCode.NMDP, RegistryCode.FRANCE}};

            var result = matchFilteringService.FulfilsRegistryCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsSearchTypeCriteria_ForMatchOfSpecifiedType_ReturnsTrue()
        {
            const DonorType donorType = DonorType.Cord;
            var match = new MatchResult {Donor = new DonorResult {DonorType = donorType}};
            var criteria = new AlleleLevelMatchCriteria {SearchType = donorType};

            var result = matchFilteringService.FulfilsSearchTypeCriteria(match, criteria);

            result.Should().BeTrue();
        }

        [Test]
        public void FulfilsSearchTypeCriteria_ForMatchOfUnSpecifiedType_ReturnsFalse()
        {
            const DonorType donorType = DonorType.Cord;
            const DonorType searchType = DonorType.Adult;
            var match = new MatchResult {Donor = new DonorResult {DonorType = donorType}};
            var criteria = new AlleleLevelMatchCriteria {SearchType = searchType};

            var result = matchFilteringService.FulfilsSearchTypeCriteria(match, criteria);

            result.Should().BeFalse();
        }

        [Test]
        public void FulfilsSearchTypeSpecificCriteria_ForAdultSearch_WithExactTotalMismatchCount_ReturnsTrue()
        {
            const DonorType searchType = DonorType.Adult;
            var match = new MatchResult {Donor = new DonorResult {DonorType = searchType}};
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
            var match = new MatchResult {Donor = new DonorResult {DonorType = searchType}};
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
            var match = new MatchResult {Donor = new DonorResult {DonorType = searchType}};
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
            var match = new MatchResult {Donor = new DonorResult {DonorType = searchType}};
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