using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Matching.Services;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.Common.Test.Matching.Services
{
    [TestFixture]
    internal class StringBasedLocusMatchCalculatorTests
    {
        private const string ArbitraryPGroup1 = "arbitrary-p-group";
        private const string ArbitraryPGroup2 = "arbitrary-p-group-2";

        private readonly LocusInfo<string> defaultPGroups = new LocusInfo<string>(ArbitraryPGroup1);


        private IStringBasedLocusMatchCalculator matchCalculator;

        [SetUp]
        public void SetUp()
        {
            matchCalculator = new StringBasedLocusMatchCalculator();
        }

        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void MatchCount_WhenSingleDonorPositionNull_ThrowsException(LocusPosition position)
        {
            var donorHla = new LocusInfoBuilder<string>()
                .WithDataAt(position, null)
                .WithDataAt(position.Other(), ArbitraryPGroup1)
                .Build();

            Assert.Throws<ArgumentException>(() => matchCalculator.MatchCount(defaultPGroups, donorHla));
        }

        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void MatchCount_WhenSinglePatientPositionNull_ThrowsException(LocusPosition position)
        {
            var patientHla = new LocusInfoBuilder<string>()
                .WithDataAt(position, null)
                .WithDataAt(position.Other(), ArbitraryPGroup1)
                .Build();

            Assert.Throws<ArgumentException>(() => matchCalculator.MatchCount(patientHla, defaultPGroups));
        }

        [Test]
        public void MatchCount_WhenNoPositionsMatch_ReturnsZero()
        {
            var patientHla = new LocusInfo<string>("p1", "p2");
            var donorHla = new LocusInfo<string>("p3", "p4");

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(0);
        }

        [Test]
        public void MatchCount_WhenInputUntyped_ReturnsTwo()
        {
            var untypedLocus = new LocusInfo<string>(null as string);
            var patientHla = new LocusInfo<string>("p1", "p2");
            var donorHla = new LocusInfo<string>("p3", "p4");

            matchCalculator.MatchCount(patientHla, untypedLocus).Should().Be(2);
            matchCalculator.MatchCount(untypedLocus, donorHla).Should().Be(2);
            matchCalculator.MatchCount(untypedLocus, untypedLocus).Should().Be(2);
        }

        [Test]
        public void MatchCount_WhenInputUntyped_AndUntypedLocusBehaviourSetToMismatch_ReturnsZero()
        {
            const UntypedLocusBehaviour untypedLocusBehaviour = UntypedLocusBehaviour.TreatAsMismatch;

            var untypedLocus = new LocusInfo<string>(null as string);
            var patientHla = new LocusInfo<string>("p1", "p2");
            var donorHla = new LocusInfo<string>("p3", "p4");

            matchCalculator.MatchCount(patientHla, untypedLocus, untypedLocusBehaviour).Should().Be(0);
            matchCalculator.MatchCount(untypedLocus, donorHla, untypedLocusBehaviour).Should().Be(0);
            matchCalculator.MatchCount(untypedLocus, untypedLocus, untypedLocusBehaviour).Should().Be(0);
        }

        [Test]
        public void MatchCount_WhenInputUntyped_AndUntypedLocusBehaviourSetToThrow_Throws()
        {
            const UntypedLocusBehaviour untypedLocusBehaviour = UntypedLocusBehaviour.Throw;

            var untypedLocus = new LocusInfo<string>(null as string);
            var patientHla = new LocusInfo<string>("p1", "p2");
            var donorHla = new LocusInfo<string>("p3", "p4");

            matchCalculator.Invoking(m => m.MatchCount(patientHla, untypedLocus, untypedLocusBehaviour))
                .Should().Throw<ArgumentException>();
            matchCalculator.Invoking(m => m.MatchCount(untypedLocus, donorHla, untypedLocusBehaviour))
                .Should().Throw<ArgumentException>();
            matchCalculator.Invoking(m => m.MatchCount(untypedLocus, untypedLocus, untypedLocusBehaviour))
                .Should().Throw<ArgumentException>();
        }

        [Test]
        public void MatchCount_ForDoubleDirectMatch_ReturnsTwo()
        {
            var patientHla = new LocusInfo<string>(ArbitraryPGroup1, ArbitraryPGroup2);
            var donorHla = new LocusInfo<string>(ArbitraryPGroup1, ArbitraryPGroup2);

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(2);
        }

        [Test]
        public void MatchCount_ForDoubleCrossMatch_ReturnsTwo()
        {
            var patientHla = new LocusInfo<string>(ArbitraryPGroup1, ArbitraryPGroup2);
            var donorHla = new LocusInfo<string>(ArbitraryPGroup2, ArbitraryPGroup1);

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(2);
        }

        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void MatchCount_ForSingleDirectMatch_ReturnsOne(LocusPosition matchPosition)
        {
            var patientHla = new LocusInfoBuilder<string>()
                .WithDataAt(matchPosition, ArbitraryPGroup1)
                .WithDataAt(matchPosition.Other(), "not-a-match-patient")
                .Build();
            var donorHla = new LocusInfoBuilder<string>()
                .WithDataAt(matchPosition, ArbitraryPGroup1)
                .WithDataAt(matchPosition.Other(), "not-a-match-donor")
                .Build();

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(1);
        }

        [TestCase(LocusPosition.One)]
        [TestCase(LocusPosition.Two)]
        public void MatchCount_ForSingleCrossMatch_ReturnsOne(LocusPosition positionOfMatchingHlaForPatient)
        {
            var patientHla = new LocusInfoBuilder<string>()
                .WithDataAt(positionOfMatchingHlaForPatient, ArbitraryPGroup1)
                .WithDataAt(positionOfMatchingHlaForPatient.Other(), "not-a-match-patient")
                .Build();
            var donorHla = new LocusInfoBuilder<string>()
                .WithDataAt(positionOfMatchingHlaForPatient.Other(), ArbitraryPGroup1)
                .WithDataAt(positionOfMatchingHlaForPatient, "not-a-match-donor")
                .Build();

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenOneDonorPositionMatchesBothPatientPositions_ReturnsOne()
        {
            var patientHla = new LocusInfo<string>(ArbitraryPGroup1);
            var donorHla = new LocusInfo<string>(ArbitraryPGroup1, "not-a-match");

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(1);
        }

        [Test]
        public void MatchCount_WhenOnePatientPositionMatchesBothDonorPositions_ReturnsOne()
        {
            var patientHla = new LocusInfo<string>(ArbitraryPGroup1, "not-a-match");
            var donorHla = new LocusInfo<string>(ArbitraryPGroup1);

            matchCalculator.MatchCount(patientHla, donorHla).Should().Be(1);
        }

        [Test]
        // Each iteration runs 7 variations of the calculation
        [Repeat(100_000)]
        [IgnoreExceptOnCiPerfTest("Ran in ~1s")]
        public void MatchCount_PerformanceTest()
        {
            var nonMatchingHla = new List<LocusInfo<string>>
            {
                new LocusInfo<string>(ArbitraryPGroup1, "not-a-match"),
                new LocusInfo<string>("not-a-match", ArbitraryPGroup1),
                new LocusInfo<string>("not-a-match", "not-a-match-2")
            };
            matchCalculator.MatchCount(defaultPGroups, defaultPGroups);
            foreach (var hla in nonMatchingHla)
            {
                matchCalculator.MatchCount(defaultPGroups, hla);
                matchCalculator.MatchCount(hla, defaultPGroups);
            }
        }
    }
}