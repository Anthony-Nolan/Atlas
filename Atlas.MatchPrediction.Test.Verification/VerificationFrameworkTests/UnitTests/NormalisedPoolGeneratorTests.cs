using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.ExternalInterface;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Services;
using Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.TestHelpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

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

            generator = new NormalisedPoolGenerator(reader, poolRepository);
        }

        [Test]
        public async Task GenerateNormalisedHaplotypeFrequencyPool_ReturnsNormalisedHaplotypeForEachHaplotype()
        {
            const int haplotypeCount = 12;

            reader.GetActiveGlobalHaplotypeFrequencies()
                .ReturnsForAnyArgs(HaplotypeFrequencyBuilder.Default.Build(haplotypeCount).ToList());

            var results = (await generator.GenerateNormalisedHaplotypeFrequencyPool()).ToList();

            results.Count.Should().Be(haplotypeCount);
        }

        [Test]
        public async Task GenerateNormalisedHaplotypeFrequencyPool_CalculatesCorrectCopyNumber()
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
            
            reader.GetActiveGlobalHaplotypeFrequencies().ReturnsForAnyArgs(new[] {
                lowestFrequency,
                middleFrequency,
                highestFrequency
            });
            
            var results = (await generator.GenerateNormalisedHaplotypeFrequencyPool())
                .OrderBy(x => x.Frequency)
                .ToList();

            results[0].CopyNumber.Should().Be(lowestFrequencyCopyNumber);
            results[1].CopyNumber.Should().Be(middleFrequencyCopyNumber);
            results[2].CopyNumber.Should().Be(highestFrequencyCopyNumber);
        }
    }
}
