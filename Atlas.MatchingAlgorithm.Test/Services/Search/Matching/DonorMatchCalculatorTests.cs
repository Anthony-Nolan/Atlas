using System;
using System.Collections.Generic;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Matching;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Matching
{
    [TestFixture]
    public class DonorMatchCalculatorTests
    {
        private IDonorMatchCalculator donorMatchCalculator;

        // ReSharper disable once InconsistentNaming
        private const string PatientPGroup1_1 = "p-group1-1";
        // ReSharper disable once InconsistentNaming
        private const string PatientPGroup1_2 = "p-group1-2";
        private const string PatientPGroup2 = "p-group2";
        private const string PatientPGroupHomozygous = "p-group-shared";

        private const string NonMatchingPGroup = "p-group-that-does-not-match-either-patient-p-group";
        private const string ArbitraryPGroup = "arbitrary-p-group";

        // Use constant patient hla data to make tests shorter

        private readonly AlleleLevelLocusMatchCriteria defaultCriteria = new AlleleLevelLocusMatchCriteria()
        {
            PGroupsToMatchInPositionOne = new List<string> {PatientPGroup1_1, PatientPGroup1_2},
            PGroupsToMatchInPositionTwo = new List<string> {PatientPGroup2},
        };
        
        private readonly AlleleLevelLocusMatchCriteria homozygousPatientCriteria = new AlleleLevelLocusMatchCriteria()
        {
            PGroupsToMatchInPositionOne = new List<string> {PatientPGroupHomozygous},
            PGroupsToMatchInPositionTwo = new List<string> {PatientPGroupHomozygous},
        };    
        
        private readonly AlleleLevelLocusMatchCriteria patientWithNoPGroupsAtPositionOneCriteria = new AlleleLevelLocusMatchCriteria()
        {
            PGroupsToMatchInPositionOne = new List<string> {},
            PGroupsToMatchInPositionTwo = new List<string> {PatientPGroup2},
        };

        [SetUp]
        public void SetUp()
        {
            donorMatchCalculator = new DonorMatchCalculator();
        }

        #region MatchCount Tests
        
        [Test]
        public void CalculateMatchesForDonors_WhenNoPGroupsMatch_ReturnsMatchCountOfZero()
        {
            var donorPGroups = new List<string>{NonMatchingPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups, donorPGroups);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(0);
        }

        [Test]
        public void CalculateMatchesForDonors_WhenDonorNotTypedAtLocus_ReturnsMatchCountOfTwo()
        {
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(null, null);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForDoubleDirectMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string>{PatientPGroup1_1};
            var donorPGroups2 = new List<string>{PatientPGroup2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForDoubleCrossMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string>{PatientPGroup2};
            var donorPGroups2 = new List<string>{PatientPGroup1_1};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForSingleDirectMatchAtPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{PatientPGroup1_1};
            var donorPGroups2 = new List<string>{NonMatchingPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForSingleDirectMatchAtPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{NonMatchingPGroup};
            var donorPGroups2 = new List<string>{PatientPGroup2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenDonorPositionOneMatchesPatientPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{PatientPGroup2};
            var donorPGroups2 = new List<string>{NonMatchingPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenDonorPositionTwoMatchesPatientPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{NonMatchingPGroup};
            var donorPGroups2 = new List<string>{PatientPGroup1_2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenOneDonorPositionMatchesBothPatientPositions_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{NonMatchingPGroup};
            var donorPGroups2 = new List<string>{PatientPGroup1_2, PatientPGroup2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenBothDonorPositionMatchesOnePatientPosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{PatientPGroup2};
            var donorPGroups2 = new List<string>{PatientPGroup2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenMultiplePGroupsMatchForASinglePosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{NonMatchingPGroup};
            var donorPGroups2 = new List<string>{PatientPGroup1_1, PatientPGroup1_2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }

        [Test]
        public void CalculateMatchesForDonors_ForHomozygousPatientMatchingNeitherPosition_ReturnsMatchCountOfZero()
        {
            var donorPGroups1 = new List<string>{NonMatchingPGroup};
            var donorPGroups2 = new List<string>{NonMatchingPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(homozygousPatientCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(0);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForHomozygousPatientMatchingPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{PatientPGroupHomozygous};
            var donorPGroups2 = new List<string>{NonMatchingPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(homozygousPatientCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForHomozygousPatientMatchingPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string>{NonMatchingPGroup};
            var donorPGroups2 = new List<string>{PatientPGroupHomozygous};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(homozygousPatientCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForHomozygousPatientMatchingBothPositions_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string>{PatientPGroupHomozygous};
            var donorPGroups2 = new List<string>{PatientPGroupHomozygous};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(homozygousPatientCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }

        [Test]
        public void CalculateMatchesForDonors_WhenDonorPositionHasNoPGroups_DoesNotMatchAtThatPosition()
        {
            // This can happen in the case of a null allele
            var donorPGroups1 = new List<string>{};
            var donorPGroups2 = new List<string>{PatientPGroupHomozygous};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(homozygousPatientCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }          
        
        [Test]
        public void CalculateMatchesForDonors_WhenPatientPositionHasNoPGroups_DoesNotMatchAtThatPosition()
        {
            var donorPGroups1 = new List<string>{PatientPGroup2};
            var donorPGroups2 = new List<string>{PatientPGroup2};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(patientWithNoPGroupsAtPositionOneCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }

        #endregion
        
        [Test]
        public void CalculateMatchesForDonors_WhenOnlyDonorPositionOneNull_ThrowsException()
        {
            var donorPGroups = new List<string>{ArbitraryPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(null, donorPGroups);

            Assert.Throws<ArgumentException>(() =>donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla));
        }
              
        
        [Test]
        public void CalculateMatchesForDonors_WhenOnlyDonorPositionTwoNull_ThrowsException()
        {
            var donorPGroups = new List<string>{ArbitraryPGroup};
            var donorHla = new Tuple<IEnumerable<string>, IEnumerable<string>>(donorPGroups, null);

            Assert.Throws<ArgumentException>(() =>donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla));
        }
    }
}