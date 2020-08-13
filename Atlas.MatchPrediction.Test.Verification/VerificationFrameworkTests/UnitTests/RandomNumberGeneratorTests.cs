using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Atlas.MatchPrediction.Test.Verification.VerificationFrameworkTests.UnitTests
{
    [TestFixture]
    public class RandomNumberGeneratorTests
    {
        private IRandomNumberGenerator generator;

        [SetUp]
        public void SetUp()
        {
            generator = new RandomNumberGenerator();
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(1000)]
        public void GenerateRandomNumberPairs_ReturnsRequestedNumberOfPairs(int pairCount)
        {
            var request = new GenerateRandomNumberRequest
            {
                Count = pairCount,
                MinPermittedValue = 0,
                MaxPermittedValue = 1235
            };

            var results = generator.GenerateRandomNumberPairs(request);

            results.Count.Should().Be(pairCount);
        }

        [Test, Repeat(100)]
        public void GenerateRandomNumberPairs_OnlyReturnsNumbersWithinRequestedRange()
        {
            const int min = 25;
            const int max = 999;
            var fullRangeOffNumbers = Enumerable.Range(min, max).ToList();

            var request = new GenerateRandomNumberRequest
            {
                Count = 100,
                MinPermittedValue = min,
                MaxPermittedValue = max
            };

            var results = generator.GenerateRandomNumberPairs(request).ToList();

            results.Select(r => r.Item1).Should().BeSubsetOf(fullRangeOffNumbers);
            results.Select(r => r.Item2).Should().BeSubsetOf(fullRangeOffNumbers);
        }
    }
}
