using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using FluentAssertions;
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
                new[]
                {
                    NormalisedPoolMemberBuilder.New
                        .With(x => x.CopyNumber, totalCopyNumber)
                        .With(x => x.PoolIndexLowerBoundary, 0)
                        .Build()
                });

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
                new[]
                {
                    NormalisedPoolMemberBuilder.New
                        .With(x => x.CopyNumber, totalCopyNumber)
                        .With(x => x.PoolIndexLowerBoundary, indexBoundary)
                        .Build()
                });

            randomNumberPairGenerator.GenerateRandomNumberPairs(default, default)
                .ReturnsForAnyArgs(BuildPairsOfIdenticalNumbers(indexBoundary, requiredGenotypeCount));

            var results = genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool);

            results.Count.Should().Be(requiredGenotypeCount);
        }

        [Test]
        public void SimulateGenotypes_CorrectlyMapsHlaWhenBuildingGenotype()
        {
            // Arrange: Force simulator to build a single genotype from two haplotypes
            // that have distinct HLA at each locus, so mapping can be tested.

            var firstHaplotype = HaplotypeFrequencyBuilder.Default
                .With(x => x.A, "a-1")
                .With(x => x.B, "b-1")
                .With(x => x.C, "c-1")
                .With(x => x.Dqb1, "dqb1-1")
                .With(x => x.Drb1, "drb1-1")
                .Build();

            var secondHaplotype = HaplotypeFrequencyBuilder.Default
                .With(x => x.A, "a-2")
                .With(x => x.B, "b-2")
                .With(x => x.C, "c-2")
                .With(x => x.Dqb1, "dqb1-2")
                .With(x => x.Drb1, "drb1-2")
                .Build();

            var pool = new NormalisedHaplotypePool(
                default,
                new[]
                {
                    NormalisedPoolMemberBuilder.New
                        .With(x => x.HaplotypeFrequency, firstHaplotype)
                        .With(x => x.CopyNumber, 1)
                        .With(x => x.PoolIndexLowerBoundary, 0)
                        .Build(),
                    NormalisedPoolMemberBuilder.New
                        .With(x => x.HaplotypeFrequency, secondHaplotype)
                        .With(x => x.CopyNumber, 1)
                        .With(x => x.PoolIndexLowerBoundary, 1)
                        .Build()
                });

            randomNumberPairGenerator.GenerateRandomNumberPairs(default, default)
                .ReturnsForAnyArgs(new[] {new UnorderedPair<int>(0, 1)});

            var result = genotypeSimulator.SimulateGenotypes(1, pool).Single();

            result.A_1.Should().Be("a-1");
            result.A_2.Should().Be("a-2");
            result.B_1.Should().Be("b-1");
            result.B_2.Should().Be("b-2");
            result.C_1.Should().Be("c-1");
            result.C_2.Should().Be("c-2");
            result.Dqb1_1.Should().Be("dqb1-1");
            result.Dqb1_2.Should().Be("dqb1-2");
            result.Drb1_1.Should().Be("drb1-1");
            result.Drb1_2.Should().Be("drb1-2");
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

            var pool = new NormalisedHaplotypePool(default, new[] {firstMember, secondMember});

            // use implemented random number pair generator to generate return object
            randomNumberPairGenerator.GenerateRandomNumberPairs(default, default).ReturnsForAnyArgs(
                new RandomNumberPairGenerator().GenerateRandomNumberPairs(
                    requiredGenotypeCount, secondMember.PoolIndexUpperBoundary));

            var results = genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool).ToList();

            results.Select(r => r.A_1).Distinct().Should().BeEquivalentTo(firstHlaA, secondHlaA);
            results.Select(r => r.A_2).Distinct().Should().BeEquivalentTo(firstHlaA, secondHlaA);
        }

        private static IReadOnlyCollection<UnorderedPair<int>> BuildPairsOfIdenticalNumbers(int number, int count)
        {
            return Enumerable.Range(0, count).Select(i => new UnorderedPair<int>(number, number)).ToList();
        }
    }
}