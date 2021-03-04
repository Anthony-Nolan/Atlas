using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Matching.Services
{
    [TestFixture]
    public class LocusMatchCalculatorTests
    {
        private ILocusMatchCalculator locusMatchCalculator;

        // Used p-groups in the tests as that's the most common use case, the class is not restricted to p-groups

        // ReSharper disable InconsistentNaming
        private const string PatientPGroup1_1 = "p-group1-1";

        private const string PatientPGroup1_2 = "p-group1-2";
        // ReSharper restore InconsistentNaming

        private const string PatientPGroup2 = "p-group2";
        private const string PatientPGroupHomozygous = "p-group-shared";
        private const string ArbitraryPGroup = "arbitrary-p-group";
        private const string NonMatchingPGroup = "p-group-that-does-not-match-either-patient-p-group";

        // Use constant patient hla data to make tests shorter

        private readonly LocusInfo<IEnumerable<string>> homozygousHla = new LocusInfo<IEnumerable<string>>
        (
            new List<string> {PatientPGroupHomozygous},
            new List<string> {PatientPGroupHomozygous}
        );

        private readonly LocusInfo<IEnumerable<string>> defaultHla = new LocusInfo<IEnumerable<string>>
        (
            new List<string> {PatientPGroup1_1, PatientPGroup1_2},
            new List<string> {PatientPGroup2}
        );

        private readonly LocusInfo<IEnumerable<string>> hlaWithNoAllelesAtPositionOne =
            new LocusInfo<IEnumerable<string>>
            (
                new List<string>(),
                new List<string> {PatientPGroup2}
            );

        [SetUp]
        public void SetUp()
        {
            locusMatchCalculator = new LocusMatchCalculator();
        }

        [Test]
        public void MatchCount_WhenOnlyDonorPositionOneNull_ThrowsException()
        {
            var donorPGroups = new List<string> {ArbitraryPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(null, donorPGroups);

            Assert.Throws<ArgumentException>(() => locusMatchCalculator.MatchCount(defaultHla, donorHla));
        }

        [Test]
        public void MatchCount_WhenOnlyDonorPositionTwoNull_ThrowsException()
        {
            var donorPGroups = new List<string> {ArbitraryPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups, null);

            Assert.Throws<ArgumentException>(() => locusMatchCalculator.MatchCount(defaultHla, donorHla));
        }

        [Test]
        public void MatchCount_WhenNoPGroupsMatch_ReturnsMatchCountOfZero()
        {
            var donorPGroups = new List<string> {NonMatchingPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(0);
        }

        [Test]
        public void MatchCount_WhenDonorNotTypedAtLocus_ReturnsMatchCountOfTwo()
        {
            var donorHla = new LocusInfo<IEnumerable<string>>(null as IEnumerable<string>);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_ForDoubleDirectMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string> {PatientPGroup1_1};
            var donorPGroups2 = new List<string> {PatientPGroup2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_ForDoubleCrossMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string> {PatientPGroup2};
            var donorPGroups2 = new List<string> {PatientPGroup1_1};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_ForSingleDirectMatchAtPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {PatientPGroup1_1};
            var donorPGroups2 = new List<string> {NonMatchingPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForSingleDirectMatchAtPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {NonMatchingPGroup};
            var donorPGroups2 = new List<string> {PatientPGroup2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenDonorPositionOneMatchesPatientPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {PatientPGroup2};
            var donorPGroups2 = new List<string> {NonMatchingPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenDonorPositionTwoMatchesPatientPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {NonMatchingPGroup};
            var donorPGroups2 = new List<string> {PatientPGroup1_2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenOneDonorPositionMatchesBothPatientPositions_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {NonMatchingPGroup};
            var donorPGroups2 = new List<string> {PatientPGroup1_2, PatientPGroup2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenBothDonorPositionMatchesOnePatientPosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {PatientPGroup2};
            var donorPGroups2 = new List<string> {PatientPGroup2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenMultiplePGroupsMatchForASinglePosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {NonMatchingPGroup};
            var donorPGroups2 = new List<string> {PatientPGroup1_1, PatientPGroup1_2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingNeitherPosition_ReturnsMatchCountOfZero()
        {
            var donorPGroups1 = new List<string> {NonMatchingPGroup};
            var donorPGroups2 = new List<string> {NonMatchingPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(0);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {PatientPGroupHomozygous};
            var donorPGroups2 = new List<string> {NonMatchingPGroup};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> {NonMatchingPGroup};
            var donorPGroups2 = new List<string> {PatientPGroupHomozygous};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingBothPositions_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string> {PatientPGroupHomozygous};
            var donorPGroups2 = new List<string> {PatientPGroupHomozygous};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_WhenDonorPositionHasNoPGroups_DoesNotMatchAtThatPosition()
        {
            // This can happen in the case of a null allele
            var donorPGroups1 = new List<string> { };
            var donorPGroups2 = new List<string> {PatientPGroupHomozygous};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenPatientPositionHasNoPGroups_DoesNotMatchAtThatPosition()
        {
            var donorPGroups1 = new List<string> {PatientPGroup2};
            var donorPGroups2 = new List<string> {PatientPGroup2};
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = locusMatchCalculator.MatchCount(hlaWithNoAllelesAtPositionOne, donorHla);

            matchDetails.Should().Be(1);
        }
    }
}