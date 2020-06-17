using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Matching
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
        private const string ArbitraryPGroup = "arbitrary-p-group";

        // Use constant patient hla data to make tests shorter

        private readonly AlleleLevelLocusMatchCriteria defaultCriteria = new AlleleLevelLocusMatchCriteria()
        {
            PGroupsToMatchInPositionOne = new List<string> {PatientPGroup1_1, PatientPGroup1_2},
            PGroupsToMatchInPositionTwo = new List<string> {PatientPGroup2},
        };

        [SetUp]
        public void SetUp()
        {
            var alleleGroupsMatchingCount = Substitute.For<IAlleleGroupsMatchingCount>();
            donorMatchCalculator = new DonorMatchCalculator(alleleGroupsMatchingCount);
        }

        [Test]
        public void CalculateMatchesForDonors_WhenOnlyDonorPositionOneNull_ThrowsException()
        {
            var donorPGroups = new List<string>{ArbitraryPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(null, donorPGroups);

            Assert.Throws<ArgumentException>(() =>donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla));
        }
              
        
        [Test]
        public void CalculateMatchesForDonors_WhenOnlyDonorPositionTwoNull_ThrowsException()
        {
            var donorPGroups = new List<string>{ArbitraryPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups, null);

            Assert.Throws<ArgumentException>(() =>donorMatchCalculator.CalculateMatchDetailsForDonorHla(defaultCriteria, donorHla));
        }
    }
}