using System;
using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Services.Matching;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services
{
    [TestFixture]
    public class DonorMatchCalculatorTests : TestBase<DonorMatchCalculator>
    {
        private IDonorMatchCalculator donorMatchCalculator;

        private const string PatientPGroup1_1 = "p-group1-1";
        private const string PatientPGroup1_2 = "p-group1-2";

        private const string PatientPGroup2 = "p-group2";

        // Use constant patient hla data to make tests shorter
        private readonly AlleleLevelLocusMatchCriteria defaultCriteria = new AlleleLevelLocusMatchCriteria()
        {
            HlaNamesToMatchInPositionOne = new List<string> {PatientPGroup1_1, PatientPGroup1_2},
            HlaNamesToMatchInPositionTwo = new List<string> {PatientPGroup2},
        };

        [SetUp]
        public void SetUp()
        {
            donorMatchCalculator = new DonorMatchCalculator();
        }

        [Test]
        public void CalculateMatchesForDonors_WhenNoPGroupsMatch_ReturnsMatchCountOfZero()
        {
            var donorPGroups = new ExpandedHla {PGroups = new List<string>{"DOES NOT MATCH"}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups, donorPGroups);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(0);
        }

        [Test]
        public void CalculateMatchesForDonors_WhenBothExpandedHlaNull_ReturnsMatchCountOfTwo()
        {
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(null, null);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        // This should never happen, but if either hla data at a locus is missing, we will assume it is untyped
        [Test]
        public void CalculateMatchesForDonors_WhenOneExpandedHlaNullAndOtherDoesNotMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups = new ExpandedHla {PGroups = new List<string>{"DOES NOT MATCH"}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(null, donorPGroups);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForDoubleDirectMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{PatientPGroup1_1}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup2}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForDoubleCrossMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{PatientPGroup2}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup1_1}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(2);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForSingleDirectMatchAtPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{PatientPGroup1_1}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{"NOT A MATCH"}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_ForSingleDirectMatchAtPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{"NOT A MATCH"}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup2}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenDonorPositionOneMatchesPatientPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{PatientPGroup2}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{""}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenDonorPositionTwoMatchesPatientPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{""}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup1_2}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenOneDonorPositionMatchesBothPatientPositions_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{""}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup1_2, PatientPGroup2}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenBothDonorPositionMatchesOnePatientPosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{PatientPGroup2}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup2}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
        
        [Test]
        public void CalculateMatchesForDonors_WhenMultiplePGroupsMatchForASinglePosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new ExpandedHla {PGroups = new List<string>{""}};
            var donorPGroups2 = new ExpandedHla {PGroups = new List<string>{PatientPGroup1_1, PatientPGroup1_2}};
            var donorHla = new Tuple<ExpandedHla, ExpandedHla>(donorPGroups1, donorPGroups2);

            var matchDetails = donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla);

            matchDetails.MatchCount.Should().Be(1);
        }
    }
}