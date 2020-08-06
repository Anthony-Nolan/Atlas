using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class NormalisedPoolGeneratorTests
    {
        private INormalisedPoolGenerator generator;
        private IHaplotypeFrequenciesReader reader;
        private INormalisedPoolRepository poolRepository;

        [SetUp]
        public void Setup()
        {
            reader = Substitute.For<IHaplotypeFrequenciesReader>();
            poolRepository = Substitute.For<INormalisedPoolRepository>();

            generator = new NormalisedPoolGenerator(reader, poolRepository, "sql-server");
        }

        [Test]
        public async Task GenerateNormalisedHaplotypeFrequencyPool_ReturnsPoolMemberForEachHaplotype()
        {
            const int haplotypeCount = 12;

            reader.GetUnalteredActiveGlobalHaplotypeFrequencies()
                .ReturnsForAnyArgs(new HaplotypeFrequenciesReaderResult
                {
                    HaplotypeFrequencySetId = 0,
                    HaplotypeFrequencies = HaplotypeFrequencyBuilder.Default.Build(haplotypeCount).ToList()
                });

            var results = await generator.GenerateNormalisedHaplotypeFrequencyPool();

            results.PoolMembers.Count.Should().Be(haplotypeCount);
        }

        [Test]
        public async Task GenerateNormalisedHaplotypeFrequencyPool_CalculatesCorrectCopyNumbers()
        {
            const int lowestFrequencyCopyNumber = 1;
            const int middleFrequencyCopyNumber = 2;
            const int highestFrequencyCopyNumber = 12345;

            var lowestFrequency = HaplotypeFrequencyBuilder.Default.Build();
            var middleFrequency = HaplotypeFrequencyBuilder.Default
                .With(x => x.Frequency, lowestFrequency.Frequency * middleFrequencyCopyNumber)
                .Build();
            var highestFrequency = HaplotypeFrequencyBuilder.Default
                .With(x => x.Frequency, lowestFrequency.Frequency * highestFrequencyCopyNumber)
                .Build();

            reader.GetUnalteredActiveGlobalHaplotypeFrequencies().ReturnsForAnyArgs(new HaplotypeFrequenciesReaderResult
            {
                HaplotypeFrequencySetId = 0,
                HaplotypeFrequencies = new[] { lowestFrequency, middleFrequency, highestFrequency }
            });

            var results = await generator.GenerateNormalisedHaplotypeFrequencyPool();
            var poolMembers = results.PoolMembers.ToList();

            results.TotalCopyNumber.Should().Be(lowestFrequencyCopyNumber + middleFrequencyCopyNumber + highestFrequencyCopyNumber);
            poolMembers[0].CopyNumber.Should().Be(lowestFrequencyCopyNumber);
            poolMembers[1].CopyNumber.Should().Be(middleFrequencyCopyNumber);
            poolMembers[2].CopyNumber.Should().Be(highestFrequencyCopyNumber);
        }

        [Test]
        public async Task GenerateNormalisedHaplotypeFrequencyPool_CalculatesCorrectPoolIndexBoundaries()
        {
            const int haplotype2CopyNumber = 1;
            const int haplotype3CopyNumber = 3;
            const int haplotype4CopyNumber = 5;

            var haplotype1 = HaplotypeFrequencyBuilder.Default.Build();
            var haplotype2 = HaplotypeFrequencyBuilder.Default
                .With(x => x.Frequency, haplotype1.Frequency * haplotype2CopyNumber)
                .Build();
            var haplotype3 = HaplotypeFrequencyBuilder.Default
                .With(x => x.Frequency, haplotype1.Frequency * haplotype3CopyNumber)
                .Build();
            var haplotype4 = HaplotypeFrequencyBuilder.Default
                .With(x => x.Frequency, haplotype1.Frequency * haplotype4CopyNumber)
                .Build();

            reader.GetUnalteredActiveGlobalHaplotypeFrequencies().ReturnsForAnyArgs(new HaplotypeFrequenciesReaderResult
            {
                HaplotypeFrequencySetId = 0,
                HaplotypeFrequencies = new[] { haplotype1, haplotype2, haplotype3, haplotype4 }
            });

            var results = await generator.GenerateNormalisedHaplotypeFrequencyPool();
            var poolMembers = results.PoolMembers.ToList();

            poolMembers[0].PoolIndexLowerBoundary.Should().Be(0);
            poolMembers[0].PoolIndexUpperBoundary.Should().Be(0);

            poolMembers[1].PoolIndexLowerBoundary.Should().Be(1);
            poolMembers[1].PoolIndexUpperBoundary.Should().Be(1);

            poolMembers[2].PoolIndexLowerBoundary.Should().Be(2);
            poolMembers[2].PoolIndexUpperBoundary.Should().Be(4);

            poolMembers[3].PoolIndexLowerBoundary.Should().Be(5);
            poolMembers[3].PoolIndexUpperBoundary.Should().Be(9);
        }
    }
}
