using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using FluentAssertions;
using LochNessBuilder;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class GenotypeSimulatorTests
    {
        private IGenotypeSimulator genotypeSimulator;
        private IRandomNumberPairGenerator randomNumberPairGenerator;

        [SetUp]
        public void SetUp()
        {
            randomNumberPairGenerator = Substitute.For<IRandomNumberPairGenerator>();
            genotypeSimulator = new GenotypeSimulator(randomNumberPairGenerator);
        }

        [Test]
        public void SimulateGenotypes_GeneratesRandomNumberPairsWithCorrectArguments()
        {
            const int requiredGenotypeCount = 100;
            const int totalCopyNumber = 50;

            var pool = new NormalisedHaplotypePool(
                default,
                new[] { NormalisedPoolMemberBuilder.New
                    .With(x => x.CopyNumber, totalCopyNumber)
                    .With(x => x.PoolIndexLowerBoundary, 0)
                    .Build()});

            genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool);

            randomNumberPairGenerator.Received().GenerateRandomNumberPairs(requiredGenotypeCount, totalCopyNumber - 1);
        }

        [Test]
        public void SimulateGenotypes_ReturnsRequiredNumberOfSimulatedGenotypes()
        {
            const int requiredGenotypeCount = 100;
            const int totalCopyNumber = 1;
            const int indexBoundary = 0;

            var pool = new NormalisedHaplotypePool(
                default,
                new[] { NormalisedPoolMemberBuilder.New
                    .With(x => x.CopyNumber, totalCopyNumber)
                    .With(x => x.PoolIndexLowerBoundary, indexBoundary)
                    .Build() });

            randomNumberPairGenerator.GenerateRandomNumberPairs(default, default)
                .ReturnsForAnyArgs(BuildPairsOfIdenticalNumbers(indexBoundary, requiredGenotypeCount));

            var results = genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool);

            results.Count.Should().Be(requiredGenotypeCount);
        }

        [Test]
        public void SimulateGenotypes_SimulatedGenotypeHasHlaAtEveryPosition()
        {
            const int requiredGenotypeCount = 1;
            const int indexBoundary = 0;

            var pool = new NormalisedHaplotypePool(
                default, 
                new[] { NormalisedPoolMemberBuilder.New
                    .With(x => x.CopyNumber, 1)
                    .With(x => x.PoolIndexLowerBoundary, indexBoundary)
                    .Build() });

            randomNumberPairGenerator.GenerateRandomNumberPairs(default, default)
                .ReturnsForAnyArgs(BuildPairsOfIdenticalNumbers(indexBoundary, requiredGenotypeCount));

            var result = genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool).Single();

            result.A_1.Should().NotBeNullOrEmpty();
            result.A_2.Should().NotBeNullOrEmpty();
            result.B_1.Should().NotBeNullOrEmpty();
            result.B_2.Should().NotBeNullOrEmpty();
            result.C_1.Should().NotBeNullOrEmpty();
            result.C_2.Should().NotBeNullOrEmpty();
            result.Dqb1_1.Should().NotBeNullOrEmpty();
            result.Dqb1_2.Should().NotBeNullOrEmpty();
            result.Drb1_1.Should().NotBeNullOrEmpty();
            result.Drb1_2.Should().NotBeNullOrEmpty();
        }

        [Test, Repeat(10)]
        public void SimulateGenotypes_SimulatedGenotypesOnlyContainHlaFromHaplotypePool()
        {
            const int requiredGenotypeCount = 50;
            const string firstHlaA = "first";
            const string secondHlaA = "second";

            var firstMember = NormalisedPoolMemberBuilder.New
                .With(x => x.HaplotypeFrequency, HaplotypeFrequencyBuilder.Default.With(x => x.A, firstHlaA))
                .With(x => x.CopyNumber, 5)
                .With(x => x.PoolIndexLowerBoundary, 0)
                .Build();

            var secondMember = NormalisedPoolMemberBuilder.New
                .With(x => x.HaplotypeFrequency, HaplotypeFrequencyBuilder.Default.With(x => x.A, secondHlaA))
                .With(x => x.CopyNumber, 5)
                .With(x => x.PoolIndexLowerBoundary, firstMember.PoolIndexUpperBoundary + 1)
                .Build();

            var pool = new NormalisedHaplotypePool(default, new[] { firstMember, secondMember });

            // use implemented random number pair generator to generate return object
            randomNumberPairGenerator.GenerateRandomNumberPairs(default, default).ReturnsForAnyArgs(
                new RandomNumberPairGenerator().GenerateRandomNumberPairs(
                    requiredGenotypeCount, secondMember.PoolIndexUpperBoundary));

            var results = genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool).ToList();

            results.Select(r => r.A_1).Distinct().Should().BeEquivalentTo(firstHlaA, secondHlaA);
            results.Select(r => r.A_2).Distinct().Should().BeEquivalentTo(firstHlaA, secondHlaA);
        }

        private IReadOnlyCollection<UnorderedPair<int>> BuildPairsOfIdenticalNumbers(int number, int count)
        {
            return Builder<UnorderedPair<int>>.New
                .With(x => x.Item1, number)
                .With(x => x.Item2, number)
                .Build(count)
                .ToList();
        }
    }
}
