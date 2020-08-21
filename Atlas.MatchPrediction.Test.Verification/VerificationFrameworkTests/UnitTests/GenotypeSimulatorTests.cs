using Atlas.Common.GeneticData;
using Atlas.MatchPrediction.Test.Verification.Models;
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
        private IRandomNumberGenerator randomNumberGenerator;

        [SetUp]
        public void SetUp()
        {
            randomNumberGenerator = Substitute.For<IRandomNumberGenerator>();
            genotypeSimulator = new GenotypeSimulator(randomNumberGenerator);
        }

        [Test]
        public void SimulateGenotypes_GeneratesRandomNumberPairsWithCorrectArguments()
        {
            const int requiredGenotypeCount = 100;
            const int totalCopyNumber = 50;

            var pool = new NormalisedHaplotypePool(
                default,
                default,
                new[]
                {
                    NormalisedPoolMemberBuilder.New
                        .With(x => x.CopyNumber, totalCopyNumber)
                        .With(x => x.PoolIndexLowerBoundary, 0)
                        .Build()
                });

            genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool);

            randomNumberGenerator.Received().GenerateRandomNumberPairs(Arg.Is<GenerateRandomNumberRequest>(x => 
                x.Count == requiredGenotypeCount &&
                x.MinPermittedValue == 0 &&
                x.MaxPermittedValue == totalCopyNumber - 1
                ));
        }

        [Test]
        public void SimulateGenotypes_ReturnsRequiredNumberOfSimulatedGenotypes()
        {
            const int requiredGenotypeCount = 100;
            const int totalCopyNumber = 1;
            const int indexBoundary = 0;

            var pool = new NormalisedHaplotypePool(
                default,
                default,
                new[]
                {
                    NormalisedPoolMemberBuilder.New
                        .With(x => x.CopyNumber, totalCopyNumber)
                        .With(x => x.PoolIndexLowerBoundary, indexBoundary)
                        .Build()
                });

            randomNumberGenerator.GenerateRandomNumberPairs(default)
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

            randomNumberGenerator.GenerateRandomNumberPairs(default)
                .ReturnsForAnyArgs(new[] {new UnorderedPair<int>(0, 1)});

            var result = genotypeSimulator.SimulateGenotypes(1, pool).Single();

            result.A.Position1.Should().Be("a-1");
            result.A.Position2.Should().Be("a-2");
            result.B.Position1.Should().Be("b-1");
            result.B.Position2.Should().Be("b-2");
            result.C.Position1.Should().Be("c-1");
            result.C.Position2.Should().Be("c-2");
            result.Dqb1.Position1.Should().Be("dqb1-1");
            result.Dqb1.Position2.Should().Be("dqb1-2");
            result.Drb1.Position1.Should().Be("drb1-1");
            result.Drb1.Position2.Should().Be("drb1-2");
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

            var pool = new NormalisedHaplotypePool(default, default, new[] {firstMember, secondMember});

            // use implemented random number pair generator to generate return object
            var request = new GenerateRandomNumberRequest
            {
                Count = requiredGenotypeCount,
                MinPermittedValue = 0,
                MaxPermittedValue = secondMember.PoolIndexUpperBoundary
            };
            randomNumberGenerator.GenerateRandomNumberPairs(default).ReturnsForAnyArgs(
                new RandomNumberGenerator().GenerateRandomNumberPairs(request));

            var results = genotypeSimulator.SimulateGenotypes(requiredGenotypeCount, pool).ToList();

            results.Select(r => r.A.Position1).Distinct().Should().BeEquivalentTo(firstHlaA, secondHlaA);
            results.Select(r => r.A.Position2).Distinct().Should().BeEquivalentTo(firstHlaA, secondHlaA);
        }

        private static IReadOnlyCollection<UnorderedPair<int>> BuildPairsOfIdenticalNumbers(int number, int count)
        {
            return Enumerable.Range(0, count).Select(i => new UnorderedPair<int>(number, number)).ToList();
        }
    }
}