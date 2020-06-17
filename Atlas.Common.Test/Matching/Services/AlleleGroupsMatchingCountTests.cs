using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Matching.Services
{
    [TestFixture]
    public class AlleleGroupsMatchingCountTests
    {
        private IAlleleGroupsMatchingCount alleleGroupsMatchingCount;

        // ReSharper disable once InconsistentNaming
        private const string PatientPGroup1_1 = "p-group1-1";
        // ReSharper disable once InconsistentNaming
        private const string PatientPGroup1_2 = "p-group1-2";
        private const string PatientPGroup2 = "p-group2";
        private const string PatientPGroupHomozygous = "p-group-shared";

        private const string NonMatchingPGroup = "p-group-that-does-not-match-either-patient-p-group";

        // Use constant patient hla data to make tests shorter

        private readonly LocusInfo<IEnumerable<string>> defaultHla = new LocusInfo<IEnumerable<string>>()
        {
            Position1 = new List<string> { PatientPGroup1_1, PatientPGroup1_2 },
            Position2 = new List<string> { PatientPGroup2 },
        };

        private readonly LocusInfo<IEnumerable<string>> homozygousHla = new LocusInfo<IEnumerable<string>>()
        {
            Position1 = new List<string> { PatientPGroupHomozygous },
            Position2 = new List<string> { PatientPGroupHomozygous },
        };

        private readonly LocusInfo<IEnumerable<string>> hlaWithNoAllelesAtPositionOne = new LocusInfo<IEnumerable<string>>()
        {
            Position1 = new List<string> { },
            Position2 = new List<string> { PatientPGroup2 },
        };

        [SetUp]
        public void SetUp()
        {
            alleleGroupsMatchingCount = new AlleleGroupsMatchingCount();
        }

        [Test]
        public void MatchCount_WhenNoPGroupsMatch_ReturnsMatchCountOfZero()
        {
            var donorPGroups = new List<string> { NonMatchingPGroup };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(0);
        }

        [Test]
        public void MatchCount_WhenDonorNotTypedAtLocus_ReturnsMatchCountOfTwo()
        {
            var donorHla = new LocusInfo<IEnumerable<string>>(null);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_ForDoubleDirectMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string> { PatientPGroup1_1 };
            var donorPGroups2 = new List<string> { PatientPGroup2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_ForDoubleCrossMatch_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string> { PatientPGroup2 };
            var donorPGroups2 = new List<string> { PatientPGroup1_1 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_ForSingleDirectMatchAtPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { PatientPGroup1_1 };
            var donorPGroups2 = new List<string> { NonMatchingPGroup };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForSingleDirectMatchAtPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { NonMatchingPGroup };
            var donorPGroups2 = new List<string> { PatientPGroup2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenDonorPositionOneMatchesPatientPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { PatientPGroup2 };
            var donorPGroups2 = new List<string> { NonMatchingPGroup };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenDonorPositionTwoMatchesPatientPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { NonMatchingPGroup };
            var donorPGroups2 = new List<string> { PatientPGroup1_2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenOneDonorPositionMatchesBothPatientPositions_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { NonMatchingPGroup };
            var donorPGroups2 = new List<string> { PatientPGroup1_2, PatientPGroup2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenBothDonorPositionMatchesOnePatientPosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { PatientPGroup2 };
            var donorPGroups2 = new List<string> { PatientPGroup2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenMultiplePGroupsMatchForASinglePosition_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { NonMatchingPGroup };
            var donorPGroups2 = new List<string> { PatientPGroup1_1, PatientPGroup1_2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(defaultHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingNeitherPosition_ReturnsMatchCountOfZero()
        {
            var donorPGroups1 = new List<string> { NonMatchingPGroup };
            var donorPGroups2 = new List<string> { NonMatchingPGroup };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(0);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingPositionOne_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { PatientPGroupHomozygous };
            var donorPGroups2 = new List<string> { NonMatchingPGroup };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingPositionTwo_ReturnsMatchCountOfOne()
        {
            var donorPGroups1 = new List<string> { NonMatchingPGroup };
            var donorPGroups2 = new List<string> { PatientPGroupHomozygous };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_ForHomozygousPatientMatchingBothPositions_ReturnsMatchCountOfTwo()
        {
            var donorPGroups1 = new List<string> { PatientPGroupHomozygous };
            var donorPGroups2 = new List<string> { PatientPGroupHomozygous };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(2);
        }

        [Test]
        public void MatchCount_WhenDonorPositionHasNoPGroups_DoesNotMatchAtThatPosition()
        {
            // This can happen in the case of a null allele
            var donorPGroups1 = new List<string> { };
            var donorPGroups2 = new List<string> { PatientPGroupHomozygous };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(homozygousHla, donorHla);

            matchDetails.Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenPatientPositionHasNoPGroups_DoesNotMatchAtThatPosition()
        {
            var donorPGroups1 = new List<string> { PatientPGroup2 };
            var donorPGroups2 = new List<string> { PatientPGroup2 };
            var donorHla = new LocusInfo<IEnumerable<string>>(donorPGroups1, donorPGroups2);

            var matchDetails = alleleGroupsMatchingCount.MatchCount(hlaWithNoAllelesAtPositionOne, donorHla);

            matchDetails.Should().Be(1);
        }
    }
}
